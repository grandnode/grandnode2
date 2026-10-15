using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Server-side state of browser sessions started by an identity provider sign-in
/// </summary>
public interface IOidcSessionService
{
    /// <summary>
    ///     Records a new session and returns its id; the refresh token is kept (protected) only for a back-office account.
    ///     Null when a back-office account brings no refresh token: such a session could never be revalidated
    /// </summary>
    Task<string> Start(Customer customer, string subject, string sid, string refreshToken, string storeId);

    /// <summary>
    ///     The session may continue: not ended, of this customer, and for a back-office account within the idle and
    ///     maximum lifetime and confirmed by the provider at the configured interval (ends the session otherwise)
    /// </summary>
    Task<bool> Validate(string sessionId, Customer customer);

    Task End(string sessionId, OidcSessionEndReason reason);

    /// <summary>Ends the open sessions with the provider session id (and the subject, when given); returns them</summary>
    Task<IList<OidcSession>> EndBySid(string subject, string sid);

    /// <summary>Ends every open session of the subject; returns them</summary>
    Task<IList<OidcSession>> EndBySubject(string subject);

    /// <summary>Ends every open session of the customer (its provider link was removed); returns them</summary>
    Task<IList<OidcSession>> EndByCustomer(string customerId);

    /// <summary>Ends every open session (the links to the provider were removed); returns how many ended</summary>
    Task<int> EndAllOpen();

    /// <summary>Deletes ended sessions and sessions past their lifetime, after the retention period</summary>
    Task DeleteExpired();
}
