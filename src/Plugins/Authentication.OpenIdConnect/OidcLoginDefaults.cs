using Grand.Web.Common.Security.Oidc;

namespace Authentication.OpenIdConnect;

public static class OidcLoginDefaults
{
    /// <summary>Plugin system name; also the ExternalAuthentication link name shared with the resource server</summary>
    public const string ProviderSystemName = OidcResourceServerDefaults.ProviderSystemName;

    public const string AuthenticationScheme = "OidcLogin";
    public const string CallbackPath = "/signin-oidc";
    public const string ConfigSection = "OidcLogin";
    public const string ConfigurationUrl = "../OidcLoginSettings/Configure";
    public static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(15);
}
