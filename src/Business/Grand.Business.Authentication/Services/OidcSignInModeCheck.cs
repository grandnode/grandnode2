using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Startup check of OidcLogin:SignInMode = ProviderOnly; whether the method is active is known per store only, so the
///     sign-in policy checks that on every refusal
/// </summary>
public static class OidcSignInModeCheck
{
    public static string Problem(OidcLoginConfig config, OidcResourceServerConfig resourceServer,
        IEnumerable<PluginInfo> plugins)
    {
        if (config is not { ProviderOnly: true })
            return null;
        if (!config.IsConfigured(resourceServer))
            return "OidcResourceServer:Authority or OidcLogin:ClientId is empty";
        if (plugins?.Any(x => x.Installed &&
                              string.Equals(x.SystemName, OidcCustomerResolver.ProviderSystemName,
                                  StringComparison.OrdinalIgnoreCase)) != true)
            return "the OpenID Connect login plugin is not installed";
        return null;
    }
}
