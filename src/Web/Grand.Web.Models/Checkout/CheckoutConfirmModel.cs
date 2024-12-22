using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Checkout;

public class CheckoutConfirmModel : BaseModel
{
    public bool TermsOfServiceOnOrderConfirmPage { get; set; }
    public string MinOrderTotalWarning { get; set; }

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