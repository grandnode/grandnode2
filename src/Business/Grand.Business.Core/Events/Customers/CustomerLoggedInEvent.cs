using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Mediator;

namespace Grand.Business.Core.Events.Customers;

/// <summary>
///     Customer logged-in event
/// </summary>
public class CustomerLoggedInEvent : INotification
{
    public CustomerLoggedInEvent(Customer customer, Customer previousCustomer, string storeId, Language language)
    {
        Customer = customer;
        PreviousCustomer = previousCustomer;
        StoreId = storeId;
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
    ///     Customer the request was signed in as before the login (usually the guest)
    /// </summary>
    public Customer PreviousCustomer { get; }

    /// <summary>
    ///     Store of the request the customer logged in through
    /// </summary>
    public string StoreId { get; }

    /// <summary>
    ///     Working language of the request the customer logged in through
    /// </summary>
    public Language Language { get; }
}