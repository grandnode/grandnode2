using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Authentication.Google.Models
{
    public class ConfigurationModel : BaseModel
    {
        [GrandResourceDisplayName("Plugins.ExternalAuth.Google.ClientKeyIdentifier")]
        public string ClientKeyIdentifier { get; set; }
        [GrandResourceDisplayName("Plugins.ExternalAuth.Google.ClientSecret")]
        public string ClientSecret { get; set; }
        [GrandResourceDisplayName("Plugins.Externalauth.Google.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}
