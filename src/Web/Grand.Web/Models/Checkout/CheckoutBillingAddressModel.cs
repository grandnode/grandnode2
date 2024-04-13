using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Checkout;

public class CheckoutBillingAddressModel : BaseModel
{
    public IList<AddressModel> ExistingAddresses { get; set; } = new List<AddressModel>();
    public AddressModel BillingNewAddress { get; set; } = new();
    public bool NewAddressPreselected { get; set; }
    public string BillingAddressId { get; set; }
}