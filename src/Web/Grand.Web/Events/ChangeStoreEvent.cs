using Grand.Domain.Customers;
using MediatR;

namespace Grand.Web.Events;

public class ChangeStoreEvent : INotification
{
    public ChangeStoreEvent(Customer customer, Domain.Stores.Store store)
    {
        Customer = customer;
        Store = store;
    }

    public Customer Customer { get; private set; }
    public Domain.Stores.Store Store { get; private set; }
}