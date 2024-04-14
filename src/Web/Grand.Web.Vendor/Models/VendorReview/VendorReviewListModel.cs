using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.VendorReview;

public class VendorReviewListModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.VendorReviews.List.CreatedOnFrom")]
    [UIHint("DateNullable")]
    public DateTime? CreatedOnFrom { get; set; }

    [GrandResourceDisplayName("Vendor.VendorReviews.List.CreatedOnTo")]
    [UIHint("DateNullable")]
    public DateTime? CreatedOnTo { get; set; }

    [GrandResourceDisplayName("Vendor.VendorReviews.List.SearchText")]
    public string SearchText { get; set; }
}