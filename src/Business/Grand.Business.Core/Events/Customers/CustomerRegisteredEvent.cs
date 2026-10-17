using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Mediator;

namespace Grand.Business.Core.Events.Customers;

/// <summary>
///     Customer registered event
/// </summary>
public class CustomerRegisteredEvent : INotification
{
    public CustomerRegisteredEvent(Customer customer, Language language)
    {
        Customer = customer;
        Language = language;
    }

    /// <summary>
    ///     Customer
    /// </summary>
    public Customer Customer {
        get;
        private set;
    }

    /// <summary>
    ///     Working language of the request the customer registered through; null when the account was registered
    ///     without one (from an access token)
    /// </summary>
    public Language Language { get; }
}