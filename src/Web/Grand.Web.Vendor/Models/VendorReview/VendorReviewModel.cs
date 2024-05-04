using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.VendorReview;

public class VendorReviewModel : BaseEntityModel
{
    [GrandResourceDisplayName("Vendor.VendorReviews.Fields.Customer")]
    public string CustomerId { get; set; }

    [GrandResourceDisplayName("Vendor.VendorReviews.Fields.Customer")]
    public string CustomerInfo { get; set; }

    [GrandResourceDisplayName("Vendor.VendorReviews.Fields.Title")]
    public string Title { get; set; }

    [GrandResourceDisplayName("Vendor.VendorReviews.Fields.ReviewText")]
    public string ReviewText { get; set; }

    [GrandResourceDisplayName("Vendor.VendorReviews.Fields.Rating")]
    public int Rating { get; set; }

    [GrandResourceDisplayName("Vendor.VendorReviews.Fields.IsApproved")]
    public bool IsApproved { get; set; }

    [GrandResourceDisplayName("Vendor.VendorReviews.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }
}