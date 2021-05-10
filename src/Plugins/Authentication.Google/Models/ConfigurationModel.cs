using Grand.Infrastructure.Models;

namespace Authentication.Google.Models
{
    public class ConfigurationModel : BaseModel
    {
        public string ClientKeyIdentifier { get; set; }
        public string ClientSecret { get; set; }
    }
}
