using Grand.Business.Core.Utilities.Authentication;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Exchanges a refresh token at the identity provider (implemented by the OpenID Connect login plugin; with no
///     implementation registered a revalidation counts as refused)
/// </summary>
public interface IOidcTokenRefresher
{
    Task<OidcRefreshResult> Refresh(string refreshToken, CancellationToken cancellationToken = default);
}
