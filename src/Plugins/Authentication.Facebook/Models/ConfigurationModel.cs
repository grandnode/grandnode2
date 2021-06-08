using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Authentication.Facebook.Models
{
    public class ConfigurationModel : BaseModel
    {
        [GrandResourceDisplayName("Authentication.Facebook.ClientKeyIdentifier")]
        public string ClientId { get; set; }

        [GrandResourceDisplayName("Authentication.Facebook.ClientSecret")]
        public string ClientSecret { get; set; }

        [GrandResourceDisplayName("Authentication.Facebook.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}