using Grand.Web.Models.Contact;
using MediatR;

namespace Grand.Web.Commands.Models.Contact;

public class ContactUsSendCommand : IRequest<ContactUsModel>
{
    public ContactUsModel Model { get; set; }
    public string IpAddress { get; set; }
}