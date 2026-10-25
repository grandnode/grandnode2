using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Checkout;

public class CheckoutConfirmModel : BaseModel
{
    public bool TermsOfServiceOnOrderConfirmPage { get; set; }
    public string MinOrderTotalWarning { get; set; }

    /// <summary>
    ///     The order approval state of the cart; null when the order needs no approval
    /// </summary>
    public OrderApprovalStateModel OrderApproval { get; set; }

    public IList<string> Warnings { get; set; } = new List<string>();
    public OrderReviewDataModel OrderReviewData { get; set; } = new();

    public class OrderApprovalStateModel : BaseModel
    {
        /// <summary>
        ///     The order total is over the customer's limit, so an approval is needed
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        ///     A request is waiting for a decision
        /// </summary>
        public bool Pending { get; set; }

        /// <summary>
        ///     The cart is approved; the order can be placed
        /// </summary>
        public bool Approved { get; set; }

        public string Message { get; set; }
        public string PendingSince { get; set; }
        public string ApprovalId { get; set; }
    }

    public class OrderReviewDataModel : BaseModel
    {
        public AddressModel BillingAddress { get; set; } = new();

        public bool IsShippable { get; set; }
        public AddressModel ShippingAddress { get; set; } = new();
        public bool SelectedPickUpInStore { get; set; }
        public AddressModel PickupAddress { get; set; } = new();
        public string ShippingMethod { get; set; }
        public string ShippingAdditionDescription { get; set; }

        public string PaymentMethod { get; set; }
    }
}