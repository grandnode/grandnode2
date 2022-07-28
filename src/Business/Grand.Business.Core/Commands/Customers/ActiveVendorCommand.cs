﻿using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Core.Commands.Customers
{
    public class ActiveVendorCommand : IRequest<bool>
    {
        public ActiveVendorCommand()
        {
            CustomerIds = new List<string>();
        }
        public Vendor Vendor { get; set; }
        public bool Active { get; set; }
        public IList<string> CustomerIds { get; set; }
    }
}
