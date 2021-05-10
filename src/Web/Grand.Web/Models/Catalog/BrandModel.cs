using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;
using System.Collections.Generic;

namespace Grand.Web.Models.Catalog
{
    public partial class BrandModel : BaseEntityModel
    {
        public BrandModel()
        {
            PictureModel = new PictureModel();
            Products = new List<ProductOverviewModel>();
            PagingFilteringContext = new CatalogPagingFilteringModel();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string BottomDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string MetaDescription { get; set; }
        public string MetaTitle { get; set; }
        public string SeName { get; set; }
        public string Icon { get; set; }
        public PictureModel PictureModel { get; set; }
        public CatalogPagingFilteringModel PagingFilteringContext { get; set; }
        public IList<ProductOverviewModel> Products { get; set; }
    }

    public partial class BrandBriefInfoModel : BaseEntityModel
    {
        public string Name { get; set; }

        public string SeName { get; set; }
    }
}