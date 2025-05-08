using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Web.Models.Contact;
using MediatR;

namespace Grand.Web.Commands.Models.Contact;

public class ContactUsCommand : IRequest<ContactUsModel>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Language Language { get; set; }
    public ContactUsModel Model { get; set; }
}