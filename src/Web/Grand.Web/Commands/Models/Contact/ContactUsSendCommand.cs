using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Contact;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Contact;

public class ContactUsSendCommand : IRequest<ContactUsModel>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Domain.Stores.DomainHost Host { get; set; }
    public required Language Language { get; set; }
    public required ContactUsModel Model { get; set; }
    public string? IpAddress { get; set; }
}