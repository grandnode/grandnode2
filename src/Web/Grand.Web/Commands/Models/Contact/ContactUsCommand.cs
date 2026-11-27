using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Web.Models.Contact;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Contact;

public class ContactUsCommand : IRequest<ContactUsModel>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Language Language { get; set; }
    public ContactUsModel? Model { get; set; }
}