using System.Collections.Generic;
using System.Linq;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents;
using Orchard.Core.Contents.Settings;
using Orchard.Data.Migration;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Security;
using Orchard.Roles.Constants;
using ContentsPermissions = Orchard.Core.Contents.Permissions;

namespace Orchard.Roles {
    public class RolesDataMigration : DataMigrationImpl {
        private readonly IAuthorizationService _authorizationService;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IRoleService _roleService;

        public RolesDataMigration(IRoleService roleService,
            IAuthorizationService authorizationService,
            IContentDefinitionManager contentDefinitionManager) {
            _authorizationService = authorizationService;
            _contentDefinitionManager = contentDefinitionManager;
            _roleService = roleService;
        }

        public int Create() {
            SchemaBuilder.CreateTable("PermissionRecord",
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<string>("Name")
                    .Column<string>("FeatureName")
                    .Column<string>("Description")
                );

            SchemaBuilder.CreateTable("RoleRecord",
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<string>("Name")
                );

            SchemaBuilder.CreateTable("RolesPermissionsRecord",
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<int>("Role_id")
                    .Column<int>("Permission_id")
                    .Column<int>("RoleRecord_Id")
                );

            SchemaBuilder.CreateTable("UserRolesPartRecord",
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<int>("UserId")
                    .Column<int>("Role_id")
                );

            return 2;
        }

        public int UpdateFrom1() {

            // creates default permissions for Orchard v1.4 instances and earlier
            _roleService.CreatePermissionForRole(SystemRoles.Anonymous, ContentsPermissions.ViewContent.Name);
            _roleService.CreatePermissionForRole(SystemRoles.Authenticated, ContentsPermissions.ViewContent.Name);

            return 2;
        }
        public int UpdateFrom2() {
            //Assigns the "Create Permission" to all roles able to create contents
            var contentEditPermissions = new[]  {
                ContentsPermissions.EditContent,
                ContentsPermissions.EditOwnContent
            };

            var dynamicPermissions = new DynamicPermissions(_contentDefinitionManager);
            var securableTypes = _contentDefinitionManager.ListTypeDefinitions()
                        .Where(ctd => ctd.Settings.GetModel<ContentTypeSettings>().Securable);
            var permissionTemplates = DynamicPermissions.PermissionTemplates;
            List<object> dynContentPermissions = new List<object>();

            foreach (var typeDefinition in securableTypes) {
                dynContentPermissions.Add(new {
                    Permission = DynamicPermissions.CreateDynamicPermission(permissionTemplates[ContentsPermissions.EditContent.Name], typeDefinition),
                    CreatePermission = DynamicPermissions.CreateDynamicPermission(permissionTemplates[ContentsPermissions.CreateContent.Name], typeDefinition)
                });
                dynContentPermissions.Add(new {
                    Permission = DynamicPermissions.CreateDynamicPermission(permissionTemplates[ContentsPermissions.EditOwnContent.Name], typeDefinition),
                    CreatePermission = DynamicPermissions.CreateDynamicPermission(permissionTemplates[ContentsPermissions.CreateContent.Name], typeDefinition)
                });
            }
            var roles = _roleService.GetRoles();
            foreach (var role in roles) {
                var existingPermissionsNames = role.RolesPermissions.Select(x => x.Permission.Name).ToList();
                var checkForDynamicPermissions = true;
                var updateRole = false;
                if (existingPermissionsNames.Any(x => x == ContentsPermissions.CreateContent.Name)) {
                    continue; // Skipping this role cause it already has the Create content permission
                }
                var simulation = UserSimulation.Create(role.Name);
                foreach (var contentEditPermission in contentEditPermissions) {
                    if (_authorizationService.TryCheckAccess(contentEditPermission, simulation, null)) {
                        existingPermissionsNames.Add(ContentsPermissions.CreateContent.Name);
                        checkForDynamicPermissions = false;
                        updateRole = true;
                        break;
                    }
                }
                if (checkForDynamicPermissions) {
                    foreach (var dynContentPermission in dynContentPermissions) {
                        if (!existingPermissionsNames.Contains(((dynamic)dynContentPermission).CreatePermission.Name)) { // Skipping this permission cause it already has the Create content variation
                            if (_authorizationService.TryCheckAccess(((dynamic)dynContentPermission).Permission, simulation, null)) {
                                existingPermissionsNames.Add(((dynamic)dynContentPermission).CreatePermission.Name);
                                updateRole = true;
                            }
                        }
                    }
                }
                if (updateRole) {
                    var rolePermissionsNames = existingPermissionsNames;
                    _roleService.UpdateRole(role.Id, role.Name, rolePermissionsNames);
                }

            }
            return 3;
        }
    }
}