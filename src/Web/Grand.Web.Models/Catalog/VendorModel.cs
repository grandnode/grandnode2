using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;
using Grand.Web.Models.Vendors;

namespace Grand.Web.Models.Catalog;

public class VendorModel : BaseEntityModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public string SeName { get; set; }
    public bool AllowCustomersToContactVendors { get; set; }
    public bool RenderCaptcha { get; set; }
    public VendorAddressModel Address { get; set; } = new();
    public PictureModel PictureModel { get; set; }
    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();
    public VendorReviewOverviewModel VendorReviewOverview { get; set; }
    public IList<ProductOverviewModel> Products { get; set; } = new List<ProductOverviewModel>();
}