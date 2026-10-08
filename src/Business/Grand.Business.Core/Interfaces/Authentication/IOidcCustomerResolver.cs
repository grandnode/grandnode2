using Grand.Business.Core.Utilities.Authentication;
using System.Security.Claims;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Maps a validated access token of the identity provider to a customer: by the stored link to the token
///     subject, on first use by the verified e-mail address, otherwise by registering one
/// </summary>
public interface IOidcCustomerResolver
{
    /// <param name="principal">Principal of the validated access token</param>
    /// <param name="storeId">Store resolved from the request host</param>
    Task<OidcCustomerResult> Resolve(ClaimsPrincipal principal, string storeId);
}
