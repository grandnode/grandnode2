using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Authentication.OpenIdConnect.Models;

public class ConfigurationModel : BaseModel
{
    [GrandResourceDisplayName("Plugins.ExternalAuth.OpenIdConnect.Authority")]
    public string Authority { get; set; }

    [GrandResourceDisplayName("Plugins.ExternalAuth.OpenIdConnect.ClientId")]
    public string ClientId { get; set; }

    /// <summary>Only whether a secret is configured, never the secret itself</summary>
    [GrandResourceDisplayName("Plugins.ExternalAuth.OpenIdConnect.ClientSecret")]
    public string ClientSecret { get; set; }

    [GrandResourceDisplayName("Plugins.ExternalAuth.OpenIdConnect.DisplayOrder")]
    public int DisplayOrder { get; set; }
}
