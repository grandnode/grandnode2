using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Catalog;

public class ProductTagModel : BaseEntityModel, ILocalizedModel<ProductTagLocalizedModel>
{
    [GrandResourceDisplayName("Admin.Catalog.ProductTags.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.ProductTags.Fields.ProductCount")]
    public int ProductCount { get; set; }

    public IList<ProductTagLocalizedModel> Locales { get; set; } = new List<ProductTagLocalizedModel>();
}

public class ProductTagLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Catalog.ProductTags.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}