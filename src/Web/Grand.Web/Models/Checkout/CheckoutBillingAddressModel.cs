using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;
using System.Collections.Generic;

namespace Grand.Web.Models.Checkout
{
    public partial class CheckoutBillingAddressModel : BaseModel
    {
        public CheckoutBillingAddressModel()
        {
            ExistingAddresses = new List<AddressModel>();
            NewAddress = new AddressModel();
        }
        public IList<AddressModel> ExistingAddresses { get; set; }
        public AddressModel NewAddress { get; set; }
        public bool NewAddressPreselected { get; set; }
    }
}