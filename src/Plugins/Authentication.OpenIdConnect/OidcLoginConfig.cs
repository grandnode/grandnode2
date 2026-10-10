using Grand.Infrastructure.Configuration;

namespace Authentication.OpenIdConnect;

/// <summary>
///     appsettings section OidcLogin: the client of this store at the identity provider configured in OidcResourceServer
/// </summary>
public class OidcLoginConfig
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Scopes { get; set; } = "openid profile email";

    public bool IsConfigured(OidcResourceServerConfig resourceServer) =>
        !string.IsNullOrWhiteSpace(resourceServer?.Authority) && !string.IsNullOrWhiteSpace(ClientId);
}
