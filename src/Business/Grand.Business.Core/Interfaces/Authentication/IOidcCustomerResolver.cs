using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using System.Security.Claims;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Maps a validated access token or a browser sign-in of the identity provider to a customer: by the stored link to the token
///     subject, on first use by the verified e-mail address, otherwise by registering one
/// </summary>
public interface IOidcCustomerResolver
{
    /// <param name="principal">Principal of the validated access token</param>
    /// <param name="storeId">Store resolved from the request host</param>
    Task<OidcCustomerResult> Resolve(ClaimsPrincipal principal, string storeId);

    /// <summary>
    ///     Browser sign-in: never registers and never links a back-office account or an unverified e-mail by itself
    /// </summary>
    /// <param name="sub">Subject of the provider identity</param>
    /// <param name="email">E-mail of the provider identity</param>
    /// <param name="emailVerified">The provider verified the e-mail</param>
    /// <param name="storeId">Current store</param>
    Task<OidcLoginResult> ResolveForLogin(string sub, string email, bool emailVerified, string storeId);

    /// <summary>
    ///     Stores the link between the provider subject and the customer (one subject per customer, one customer per subject;
    ///     with per-store identity one customer per subject and store)
    /// </summary>
    /// <param name="customer">Customer to link</param>
    /// <param name="sub">Subject of the provider identity</param>
    /// <param name="email">E-mail of the provider identity</param>
    /// <param name="storeId">Current store</param>
    Task<OidcLinkResult> LinkIdentity(Customer customer, string sub, string email, string storeId);
}
