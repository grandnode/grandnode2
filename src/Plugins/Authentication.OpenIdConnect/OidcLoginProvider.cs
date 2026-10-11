using Grand.Business.Core.Interfaces.Authentication;

namespace Authentication.OpenIdConnect;

public class OidcLoginProvider(OidcLoginSettings settings) : IExternalAuthenticationProvider
{
    public string ConfigurationUrl => OidcLoginDefaults.ConfigurationUrl;

    public string SystemName => OidcLoginDefaults.ProviderSystemName;

    public string FriendlyName => "OpenID Connect";

    public int Priority => settings.DisplayOrder;

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

    /// <summary>
    ///     Gets a view component for displaying plugin in public store
    /// </summary>
    public Task<string> GetPublicViewComponentName() => Task.FromResult("OidcLogin");
}
