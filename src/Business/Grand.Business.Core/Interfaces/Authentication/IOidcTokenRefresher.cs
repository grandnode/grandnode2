using Grand.Business.Core.Utilities.Authentication;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Exchanges a refresh token at the identity provider (registered while the OpenID Connect sign-in is configured; with no
///     implementation registered a session cannot be confirmed and ends)
/// </summary>
public interface IOidcTokenRefresher
{
    Task<OidcRefreshResult> Refresh(string refreshToken, CancellationToken cancellationToken = default);
}
