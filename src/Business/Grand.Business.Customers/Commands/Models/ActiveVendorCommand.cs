using Grand.Domain.Vendors;
using MediatR;
using System.Collections.Generic;

namespace Grand.Business.Customers.Commands.Models
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
