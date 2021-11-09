using Grand.Domain.Customers;
using Grand.Domain.Stores;
using MediatR;

namespace Grand.Web.Events
{
    public class ChangeStoreEvent : INotification
    {
        public Customer Customer { get; private set; }
        public Store Store { get; private set; }

        public ChangeStoreEvent(Customer customer, Store store)
        {
            Customer = customer;
            Store = store;
        }
    }
}
