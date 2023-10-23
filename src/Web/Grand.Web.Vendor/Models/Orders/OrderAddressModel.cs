﻿using Grand.Infrastructure.Models;
using Grand.Web.Vendor.Models.Common;

namespace Grand.Web.Vendor.Models.Orders
{
    public class OrderAddressModel : BaseModel
    {
        public string OrderId { get; set; }
        public bool BillingAddress { get; set; }
        public AddressModel Address { get; set; }
    }
}