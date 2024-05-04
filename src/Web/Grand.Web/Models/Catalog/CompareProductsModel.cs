using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog;

public class CompareProductsModel : BaseEntityModel
{
    public IList<ProductOverviewModel> Products { get; set; } = new List<ProductOverviewModel>();

    public bool IncludeShortDescriptionInCompareProducts { get; set; }
    public bool IncludeFullDescriptionInCompareProducts { get; set; }
}