using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Checkout
{
    public class CheckoutConfirmModel : BaseModel
    {
        public CheckoutConfirmModel()
        {
            Warnings = new List<string>();
            OrderReviewData = new OrderReviewDataModel();
        }

        public bool TermsOfServiceOnOrderConfirmPage { get; set; }
        public string MinOrderTotalWarning { get; set; }

        public IList<string> Warnings { get; set; }
        public OrderReviewDataModel OrderReviewData { get; set; }

        public class OrderReviewDataModel : BaseModel
        {
            public OrderReviewDataModel()
            {
                BillingAddress = new AddressModel();
                ShippingAddress = new AddressModel();
                PickupAddress = new AddressModel();
            }

            public AddressModel BillingAddress { get; set; }

            public bool IsShippable { get; set; }
            public AddressModel ShippingAddress { get; set; }
            public bool SelectedPickUpInStore { get; set; }
            public AddressModel PickupAddress { get; set; }
            public string ShippingMethod { get; set; }
            public string ShippingAdditionDescription { get; set; }

            public string PaymentMethod { get; set; }

        }
    }
}