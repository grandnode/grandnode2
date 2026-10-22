namespace Grand.Web.Common.Security.Oidc;

public static class OidcResourceServerDefaults
{
    public const string AuthenticationScheme = "Oidc";

    /// <summary>ExternalAuthentication.ProviderSystemName of the link between token subject and customer; also the system name of the OpenID Connect sign-in method</summary>
    public const string ProviderSystemName = "ExternalAuth.OpenIdConnect";

    public const string CustomerItemKey = "Grand.Oidc.Customer";
    public const string FailureItemKey = "Grand.Oidc.Failure";
    public const string ResourceItemKey = "Grand.Oidc.Resource";
    public const string RouteItemKey = "Grand.Oidc.Route";
}
