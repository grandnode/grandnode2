using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Web.Services;

/// <summary>
///     The way a guest who ordered without an account opens the order and its returns: a time-limited link sent to the order's e-mail
///     address leaves a grant for that one order in the browser. Both are protected payloads stored nowhere else
/// </summary>
public interface IGuestOrderGrantService
{
    /// <summary>
    ///     The name of the grant cookie, with the prefix of the store's other cookies
    /// </summary>
    string CookieName { get; }

    /// <summary>
    ///     Whether the store lets guests open their orders by a link; the pages of the flow are not found otherwise
    /// </summary>
    bool Allowed { get; }

    /// <summary>
    ///     Whether a guest's link to an order also reaches its returns: only while the store takes them
    /// </summary>
    bool ReturnsAllowed { get; }

    /// <summary>
    ///     The token of the link sent to the order's e-mail address
    /// </summary>
    string LinkToken(Order order);

    /// <summary>
    ///     The order a link's token opens in the store; null when it expired, was changed or is of another store
    /// </summary>
    string LinkOrderId(string token, string storeId);

    /// <summary>
    ///     The customer of an order the guest flow serves: an order of the store placed without an account. A
    ///     customer who has registered since logs in instead
    /// </summary>
    Task<Customer> GuestOfOrder(Order order, string storeId);

    /// <summary>
    ///     Leaves the grant for the order in the browser, until it is closed and for the grant's lifetime at most
    /// </summary>
    void Grant(Order order);

    /// <summary>
    ///     The order the browser holds a grant for in the store; null without one
    /// </summary>
    string GrantedOrderId(string storeId);

    /// <summary>
    ///     The guest of an order placed without an account, when the browser holds the grant a link sent to the
    ///     order's e-mail address left for it; null otherwise. A visitor logged in acts on the orders of their account
    ///     only, whatever the browser holds
    /// </summary>
    Task<Customer> GrantedGuest(Order order, Customer visitor, string storeId);

    /// <summary>
    ///     The customer acting on the order: the visitor when it is theirs, else the order's guest when the browser
    ///     holds the grant a link sent to the order's e-mail address left for it; a null customer otherwise
    /// </summary>
    Task<(Customer Customer, bool GuestLink)> OrderCustomer(Order order, Customer visitor, string storeId);
}
