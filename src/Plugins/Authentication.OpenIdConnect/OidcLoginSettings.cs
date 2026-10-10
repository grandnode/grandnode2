using Grand.Domain.Configuration;

namespace Authentication.OpenIdConnect;

public class OidcLoginSettings : ISettings
{
    public int DisplayOrder { get; set; }
}
