using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.Catalog;

public class CategoryModel : BaseEntityModel
{
    public string ParentCategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string BottomDescription { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public string SeName { get; set; }
    public string Flag { get; set; }
    public string FlagStyle { get; set; }
    public string Icon { get; set; }
    public PictureModel PictureModel { get; set; } = new();
    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();
    public bool DisplayCategoryBreadcrumb { get; set; }
    public IList<CategoryModel> CategoryBreadcrumb { get; set; } = new List<CategoryModel>();
    public IList<SubCategoryModel> SubCategories { get; set; } = new List<SubCategoryModel>();
    public IList<ProductOverviewModel> FeaturedProducts { get; set; } = new List<ProductOverviewModel>();
    public IList<ProductOverviewModel> Products { get; set; } = new List<ProductOverviewModel>();

    #region Nested Classes

    public class SubCategoryModel : BaseEntityModel
    {
        public string Name { get; set; }
        public string SeName { get; set; }
        public string Description { get; set; }
        public string Flag { get; set; }
        public string FlagStyle { get; set; }
        public PictureModel PictureModel { get; set; } = new();
    }

    #endregion
}