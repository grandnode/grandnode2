namespace Grand.Web.Common.Security.Oidc;

public static class OidcResourceServerDefaults
{
    public const string AuthenticationScheme = "Oidc";

    /// <summary>ExternalAuthentication.ProviderSystemName of the link between token subject and customer</summary>
    public const string ProviderSystemName = "Oidc";

    public const string CustomerItemKey = "Grand.Oidc.Customer";
    public const string FailureItemKey = "Grand.Oidc.Failure";
    public const string ResourceItemKey = "Grand.Oidc.Resource";
}
