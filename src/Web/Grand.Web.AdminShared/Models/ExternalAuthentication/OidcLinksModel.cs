namespace Grand.Web.AdminShared.Models.ExternalAuthentication;

/// <summary>The stored OpenID Connect account links against the configured identity provider (resource server enabled)</summary>
public class OidcLinksModel
{
    public string ConfiguredAuthority { get; set; }
    public string LinkedAuthority { get; set; }
    public long LinkCount { get; set; }
    public bool ProviderChanged { get; set; }
}
