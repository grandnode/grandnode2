using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Checkout
{
    public class CheckoutBillingAddressModel : BaseModel
    {
        public CheckoutBillingAddressModel()
        {
            ExistingAddresses = new List<AddressModel>();
            BillingNewAddress = new AddressModel();
        }
        public IList<AddressModel> ExistingAddresses { get; set; }
        public AddressModel BillingNewAddress { get; set; }
        public bool NewAddressPreselected { get; set; }
        public string BillingAddressId { get; set; } 
    }
}