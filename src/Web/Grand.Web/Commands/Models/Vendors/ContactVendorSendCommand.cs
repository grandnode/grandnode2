﻿using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Web.Models.Vendors;
using MediatR;

namespace Grand.Web.Commands.Models.Vendors
{
    public class ContactVendorSendCommand : IRequest<ContactVendorModel>
    {
        public Vendor Vendor { get; set; }
        public Store Store { get; set; }
        public ContactVendorModel Model { get; set; }
        public string IpAddress { get; set; }

    }
}
