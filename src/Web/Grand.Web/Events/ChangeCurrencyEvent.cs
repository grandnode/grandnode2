using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Mediator;

namespace Grand.Web.Events;

public class ChangeCurrencyEvent : INotification
{
    public ChangeCurrencyEvent(Customer customer, Currency currency)
    {
        Customer = customer;
        Currency = currency;
    }

    public Customer Customer { get; private set; }
    public Currency Currency { get; private set; }
}