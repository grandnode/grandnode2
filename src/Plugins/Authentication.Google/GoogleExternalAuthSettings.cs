using Grand.Domain.Configuration;

namespace Authentication.Google
{
    public class GoogleExternalAuthSettings : ISettings
    {
        public string ClientKeyIdentifier { get; set; }
        public string ClientSecret { get; set; }
        public int DisplayOrder { get; set; }
    }
}
