using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Web.Models.Vendors;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Vendors;

public class InsertVendorReviewCommand : IRequest<VendorReview>
{
    public required Domain.Vendors.Vendor Vendor { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required VendorReviewsModel Model { get; set; }
}