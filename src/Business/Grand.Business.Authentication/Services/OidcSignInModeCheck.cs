using Grand.Infrastructure.Configuration;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Startup check of OidcLogin:SignInMode = ProviderOnly; whether the method is active is known per store only, so the
///     sign-in policy checks that on every refusal
/// </summary>
public static class OidcSignInModeCheck
{
    public static string Problem(OidcLoginConfig config, OidcResourceServerConfig resourceServer)
    {
        if (config is not { ProviderOnly: true })
            return null;
        if (!config.IsConfigured(resourceServer))
            return "OidcResourceServer:Authority or OidcLogin:ClientId is empty";
        return null;
    }
}
