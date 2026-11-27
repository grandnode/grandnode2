using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Vendors;

public class SetVendorReviewHelpfulnessCommand : IRequest<VendorReview>
{
    public required Customer Customer { get; set; }
    public Domain.Vendors.Vendor? Vendor { get; set; }
    public required VendorReview Review { get; set; }
    public bool Washelpful { get; set; }
}