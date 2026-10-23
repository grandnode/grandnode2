namespace Grand.Web.AdminShared.Models.ExternalAuthentication;

/// <summary>The stored OpenID Connect account links against the configured identity provider (resource server enabled)</summary>
public class OidcLinksModel
{
    public string ConfiguredAuthority { get; set; }
    public string LinkedAuthority { get; set; }
    public long LinkCount { get; set; }
    public bool ProviderChanged { get; set; }
    public bool ProviderUnknown { get; set; }

    /// <summary>Browser sign-in configured (OidcResourceServer:Authority and OidcLogin:ClientId): the sign-in card</summary>
    public bool SignInConfigured { get; set; }

    /// <summary>Resource server enabled: the account links card</summary>
    public bool LinksConfigured { get; set; }

    /// <summary>OidcLogin:ClientId of the browser sign-in</summary>
    public string ClientId { get; set; }

    /// <summary>Only whether OidcLogin:ClientSecret is set, never the secret itself</summary>
    public bool ClientSecretSet { get; set; }
}
