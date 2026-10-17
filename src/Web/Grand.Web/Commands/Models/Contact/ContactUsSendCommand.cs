using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Contact;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Contact;

public class ContactUsSendCommand : IRequest<ContactUsModel>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Domain.Stores.DomainHost Host { get; set; }
    public Language Language { get; set; }
    public ContactUsModel Model { get; set; }
    public string IpAddress { get; set; }
}