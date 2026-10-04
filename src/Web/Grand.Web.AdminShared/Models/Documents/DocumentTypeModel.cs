using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;

namespace Grand.Web.AdminShared.Models.Documents;

public class DocumentTypeModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Documents.Type.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Documents.Type.Fields.Description")]
    [SanitizeHtml]
    public string Description { get; set; }

    [GrandResourceDisplayName("Admin.Documents.Type.Fields.DisplayOrder")]

    public int DisplayOrder { get; set; }
}