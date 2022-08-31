using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Core.Events.Customers
{
    /// <summary>
    /// Vendor review approved event
    /// </summary>
    public class VendorReviewApprovedEvent : INotification
    {
        public VendorReviewApprovedEvent(VendorReview vendorReview)
        {
            VendorReview = vendorReview;
        }

        /// <summary>
        /// Vendor review
        /// </summary>
        public VendorReview VendorReview { get; private set; }
    }
}
