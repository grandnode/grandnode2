using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Tax;

public class TaxCategoryModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Configuration.Tax.Categories.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Tax.Categories.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public string StoreId { get; set; }

    public string StoreName { get; set; }
}