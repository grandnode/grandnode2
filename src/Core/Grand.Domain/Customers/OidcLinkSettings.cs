using Grand.Domain.Configuration;

namespace Grand.Domain.Customers;

/// <summary>
///     The identity provider the stored OpenID Connect links belong to (store-independent). A subject is unique only within
///     one provider, so links are not used once OidcResourceServer:Authority points elsewhere
/// </summary>
public class OidcLinkSettings : ISettings
{
    /// <summary>Authority the links were made with; empty until the first use, which adopts the configured one</summary>
    public string Authority { get; set; }
}
