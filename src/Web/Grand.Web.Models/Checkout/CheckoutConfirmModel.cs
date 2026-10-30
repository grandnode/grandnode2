using Grand.Domain.Orders;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Checkout;

public class CheckoutConfirmModel : BaseModel
{
    public bool TermsOfServiceOnOrderConfirmPage { get; set; }
    public string MinOrderTotalWarning { get; set; }

    /// <summary>
    ///     The order exceeds the buyer's order limit: it is placed at once and waits for an approver's decision
    /// </summary>
    public bool OrderApprovalRequired { get; set; }

    /// <summary>
    ///     Tells the buyer the order goes to the company for approval, with the limit formatted
    /// </summary>
    public string OrderApprovalMessage { get; set; }

    /// <summary>
    ///     Label of the confirm button when the order needs an approval
    /// </summary>
    public string OrderApprovalButtonText { get; set; }

    /// <summary>
    ///     Whether the confirm step asks for the buyer's purchase order number
    /// </summary>
    public PurchaseOrderNumberMode PurchaseOrderNumberMode { get; set; }

    /// <summary>
    ///     Maximum length of the purchase order number
    /// </summary>
    public int PurchaseOrderNumberMaxLength { get; set; }

    public IList<string> Warnings { get; set; } = new List<string>();
    public OrderReviewDataModel OrderReviewData { get; set; } = new();

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