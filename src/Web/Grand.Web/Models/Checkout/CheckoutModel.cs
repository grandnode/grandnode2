using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Checkout
{
    public partial class CheckoutModel : BaseModel
    {
        public bool ShippingRequired { get; set; }
        public CheckoutBillingAddressModel BillingAddress { get; set; }
        public CheckoutShippingAddressModel ShippingAddress { get; set; }
        public bool HasSinglePaymentMethod { get; set; }
    }
}