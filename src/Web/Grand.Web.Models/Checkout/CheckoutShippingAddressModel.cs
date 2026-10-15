using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Checkout;

public class CheckoutShippingAddressModel : BaseModel
{
    public IList<AddressModel> ExistingAddresses { get; set; } = new List<AddressModel>();
    public IList<string> Warnings { get; set; } = new List<string>();
    public AddressModel ShippingNewAddress { get; set; } = new();
    public string ShippingAddressId { get; set; }
    public string PickupPointId { get; set; }
    public bool NewAddressPreselected { get; set; }
    public IList<CheckoutPickupPointModel> PickupPoints { get; set; } = new List<CheckoutPickupPointModel>();
    public bool BillToTheSameAddress { get; set; }
    public bool AllowPickUpInStore { get; set; }

    public bool PickUpInStore { get; set; }
    public bool PickUpInStoreOnly { get; set; }
}