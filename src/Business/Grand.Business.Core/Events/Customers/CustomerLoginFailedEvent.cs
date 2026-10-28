using Grand.Domain.Customers;
using Grand.Mediator;

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

    public CustomerLoginFailedEvent(Customer customer, string reason) : this(customer)
    {
        Reason = reason;
    }

    /// <summary>
    ///     Customer
    /// </summary>
    public Customer Customer {
        get;
        private set;
    }

    /// <summary>
    ///     Why the sign-in failed, e.g. wrong credentials or a wrong two-factor code
    /// </summary>
    public string Reason { get; }
}