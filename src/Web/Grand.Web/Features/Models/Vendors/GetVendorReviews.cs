using Grand.Web.Models.Vendors;
using MediatR;

namespace Grand.Web.Features.Models.Vendors;

public class GetVendorReviews : IRequest<VendorReviewsModel>
{
    public Domain.Vendors.Vendor Vendor { get; set; }
}