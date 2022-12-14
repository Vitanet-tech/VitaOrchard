using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;

namespace Orchard.Localization {
    public class Migrations : DataMigrationImpl {

        public int Create() {
            SchemaBuilder.CreateTable("LocalizationPartRecord", 
                table => table
                    .ContentPartRecord()
                    .Column<int>("CultureId")
                    .Column<int>("MasterContentItemId")
                );

            ContentDefinitionManager.AlterPartDefinition("LocalizationPart", builder => builder.Attachable());

            return 1;
        }

        public int UpdateFrom1() {
            ContentDefinitionManager.AlterPartDefinition("LocalizationPart", builder => builder
                .WithDescription("Provides the user interface to localize content items."));

            return 2;
        }

        public int UpdateFrom2() {
            // Most searches will be interested in either the content's culture
            // or the group of contents that are localizations of one another
            SchemaBuilder.AlterTable("LocalizationPartRecord", table => {
                table.CreateIndex($"IDX_LocalizationPartRecord_CultureId",
                    "CultureId");
                table.CreateIndex($"IDX_LocalizationPartRecord_MasterContentItemId",
                    "MasterContentItemId");
            });
            return 3;
        }
    }

    [OrchardFeature("Orchard.Localization.Transliteration")]
    public class TransliterationMigrations : DataMigrationImpl {

        public int Create() {
            SchemaBuilder.CreateTable("TransliterationSpecificationRecord",
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<string>("CultureFrom")
                    .Column<string>("CultureTo")
                    .Column<string>("Rules", c => c.Unlimited())
                );

            return 1;
        }
    }
}