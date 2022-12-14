using System.Collections.Generic;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.MetaData;

namespace Orchard.ContentManagement.Drivers {
    public interface IContentPartDriver : IDependency {
        DriverResult BuildDisplay(BuildDisplayContext context);
        DriverResult BuildEditor(BuildEditorContext context);
        DriverResult UpdateEditor(UpdateEditorContext context);
        void Importing(ImportContentContext context);
        void Imported(ImportContentContext context);
        void ImportCompleted(ImportContentContext context);
        void Exporting(ExportContentContext context);
        void Exported(ExportContentContext context);
        void Cloning(CloneContentContext context);
        void Cloned(CloneContentContext context);
        IEnumerable<ContentPartInfo> GetPartInfo();
        void GetContentItemMetadata(GetContentItemMetadataContext context);
    }
}