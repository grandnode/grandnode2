using Grand.Domain.Stores;
using Grand.Web.Models.Vendors;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Vendors;

public class ContactVendorSendCommand : IRequest<ContactVendorModel>
{
    public Domain.Vendors.Vendor Vendor { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public ContactVendorModel Model { get; set; }
    public string IpAddress { get; set; }
}