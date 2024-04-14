using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Catalog;

public class ProductReviewModel : BaseEntityModel
{
    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.Product")]
    public string ProductId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.Product")]
    public string ProductName { get; set; }


    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.Store")]
    public string StoreName { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.Customer")]
    public string CustomerId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.Customer")]
    public string CustomerInfo { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.Title")]
    public string Title { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.ReviewText")]
    public string ReviewText { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.ReplyText")]
    public string ReplyText { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.Signature")]
    public string Signature { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.Rating")]
    public int Rating { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.IsApproved")]
    public bool IsApproved { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.ProductReviews.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }
}