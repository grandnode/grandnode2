using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Documents
{
    public partial class DocumentTypeModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Documents.Type.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Type.Fields.Description")]

        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Documents.Type.Fields.DisplayOrder")]

        public int DisplayOrder { get; set; }
    }
}
