using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Tax;

public class TaxCategoryModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Configuration.Tax.Categories.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Tax.Categories.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }
}