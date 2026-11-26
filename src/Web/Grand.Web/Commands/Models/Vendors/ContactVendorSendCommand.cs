using Grand.Domain.Stores;
using Grand.Web.Models.Vendors;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Vendors;

public class ContactVendorSendCommand : IRequest<ContactVendorModel>
{
    public required Domain.Vendors.Vendor Vendor { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required ContactVendorModel Model { get; set; }
    public string? IpAddress { get; set; }
}