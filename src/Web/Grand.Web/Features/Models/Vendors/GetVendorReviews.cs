using Grand.Web.Models.Vendors;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Vendors;

public class GetVendorReviews : IRequest<VendorReviewsModel>
{
    public Domain.Vendors.Vendor Vendor { get; set; }
}