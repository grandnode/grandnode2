using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Contact;
using MediatR;

namespace Grand.Web.Commands.Models.Contact;

public class ContactUsCommand : IRequest<ContactUsModel>
{
    public Customer Customer { get; set; }
    public Store Store { get; set; }
    public Language Language { get; set; }
    public ContactUsModel Model { get; set; }
}