using Grand.Business.Core.Interfaces.Authentication;
using Microsoft.AspNetCore.Authentication;

namespace Authentication.OpenIdConnect;

public class OidcLoginProvider(OidcLoginSettings settings, IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IPanelExternalAuthenticationProvider
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

    public async Task<string> GetPanelSignInPath(string returnUrl) =>
        await authenticationSchemeProvider.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme) == null
            ? null
            : OidcLoginDefaults.LoginPath + "?returnUrl=" + Uri.EscapeDataString(returnUrl ?? "/");
}
