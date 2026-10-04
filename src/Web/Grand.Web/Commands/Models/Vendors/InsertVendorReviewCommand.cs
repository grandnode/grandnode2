using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Web.Models.Vendors;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Vendors;

public class InsertVendorReviewCommand : IRequest<VendorReview>
{
    public Domain.Vendors.Vendor Vendor { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public VendorReviewsModel Model { get; set; }
}