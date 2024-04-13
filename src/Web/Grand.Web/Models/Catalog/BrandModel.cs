using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.Catalog;

public class BrandModel : BaseEntityModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string BottomDescription { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public string SeName { get; set; }
    public string Icon { get; set; }
    public PictureModel PictureModel { get; set; } = new();
    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();
    public IList<ProductOverviewModel> Products { get; set; } = new List<ProductOverviewModel>();
}

public class BrandBriefInfoModel : BaseEntityModel
{
    public string Name { get; set; }

    public string SeName { get; set; }
}