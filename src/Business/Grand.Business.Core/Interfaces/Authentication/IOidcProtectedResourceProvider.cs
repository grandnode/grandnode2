using Grand.Infrastructure.Configuration;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Lets a plugin declare the resources it protects with access tokens of the identity provider. Register the
///     implementation as a singleton in the plugin's IStartupApplication and protect the endpoints with
///     [Authorize(AuthenticationSchemes = "Oidc")].
/// </summary>
public interface IOidcProtectedResourceProvider
{
    IEnumerable<OidcProtectedResource> GetResources();
}
