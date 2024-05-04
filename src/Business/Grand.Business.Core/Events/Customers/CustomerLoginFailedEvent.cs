using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Events.Customers;

/// <summary>
///     Customer login failed event
/// </summary>
public class CustomerLoginFailedEvent : INotification
{
    public CustomerLoginFailedEvent(Customer customer)
    {
        Customer = customer;
    }

    /// <summary>
    ///     Customer
    /// </summary>
    public Customer Customer {
        get;
        private set;
    }
}