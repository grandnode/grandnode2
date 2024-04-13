using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog;

public class ProductsByTagModel : BaseEntityModel
{
    public string TagName { get; set; }
    public string TagSeName { get; set; }

    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();

    public IList<ProductOverviewModel> Products { get; set; } = new List<ProductOverviewModel>();
}