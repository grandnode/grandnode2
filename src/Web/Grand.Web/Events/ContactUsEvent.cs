using Grand.Domain.Customers;
using Grand.Web.Models.Contact;
using MediatR;

namespace Grand.Web.Events
{
    public class ContactUsEvent : INotification
    {
        public Customer Customer { get; private set; }
        public ContactUsModel Model { get; private set; }
        
        public ContactUsEvent(Customer customer, ContactUsModel model)
        {
            Customer = customer;
            Model = model;
        }
    }
}