using Grand.Domain.Customers;

namespace Grand.Web.Common.Security.Authentication;

/// <summary>
///     Authentication service interface
/// </summary>
public interface IGrandAuthenticationService
{
    /// <summary>
    ///     Sign in
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="createPersistentCookie">A value indicating whether to create a persistent cookie</param>
    Task SignIn(Customer customer, bool createPersistentCookie);

    /// <summary>
    ///     Sign in with a session started at the identity provider; the cookie references the session, so it can be ended
    ///     on the server
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="createPersistentCookie">A value indicating whether to create a persistent cookie</param>
    /// <param name="oidcSessionId">Id of the OidcSession; null or empty = an ordinary sign-in</param>
    Task SignIn(Customer customer, bool createPersistentCookie, string oidcSessionId);

    /// <summary>
    ///     Sign out
    /// </summary>
    Task SignOut();

    /// <summary>
    ///     Whether the current sign-in cookie references a session started at the identity provider; an implementation
    ///     without provider sessions keeps the default (no)
    /// </summary>
    Task<bool> HasOidcSession() => Task.FromResult(false);

    /// <summary>
    ///     Get authenticated customer
    /// </summary>
    /// <returns>Customer</returns>
    Task<Customer> GetAuthenticatedCustomer();

    /// <summary>
    ///     Get customer guid
    /// </summary>
    /// <returns>Customer</returns>
    Task<string> GetCustomerGuid();

    /// <summary>
    ///     Set customer guid
    /// </summary>
    /// <param name="customerGuid">Guid of the customer</param>
    Task SetCustomerGuid(Guid customerGuid);
}