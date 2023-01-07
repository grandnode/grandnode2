﻿using Grand.Infrastructure.Models;
using Grand.Web.Models.Common;

namespace Grand.Web.Models.Checkout
{
    public class CheckoutShippingAddressModel : BaseModel
    {
        public CheckoutShippingAddressModel()
        {
            ExistingAddresses = new List<AddressModel>();
            ShippingNewAddress = new AddressModel();
            Warnings = new List<string>();
            PickupPoints = new List<CheckoutPickupPointModel>();
        }

        public IList<AddressModel> ExistingAddresses { get; set; }
        public IList<string> Warnings { get; set; }
        public AddressModel ShippingNewAddress { get; set; }
        public string ShippingAddressId { get; set; }
        public string PickupPointId { get; set; }
        public bool NewAddressPreselected { get; set; }
        public IList<CheckoutPickupPointModel> PickupPoints { get; set; }
        public bool BillToTheSameAddress { get; set; }
        public bool AllowPickUpInStore { get; set; }

        public bool PickUpInStore { get; set; }
        public bool PickUpInStoreOnly { get; set; }
    }
}