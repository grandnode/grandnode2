namespace Grand.Web.Common.Security.Oidc;

public static class OidcLoginDefaults
{
    /// <summary>System name of the OpenID Connect sign-in method; also the ExternalAuthentication link name shared with the resource server</summary>
    public const string ProviderSystemName = OidcResourceServerDefaults.ProviderSystemName;

    public const string AuthenticationScheme = "OidcLogin";
    public const string CallbackPath = "/signin-oidc";
    public const string ConfigSection = "OidcLogin";
    public const string HttpClientName = "Grand.OidcLogin";
    public const string BackChannelLogoutPath = "oidc/backchannel-logout";
    public const string LoginPath = "/oidc/login";
    public const string FailedPath = "/oidc/failed";
    public const string ConfigurationUrl = "../ExternalAuthentication/Methods";
    public static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(15);
}
