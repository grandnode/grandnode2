using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;
using Grand.Web.Common.Security.Oidc;

namespace Authentication.OpenIdConnect;

/// <summary>
///     Sign-in with an OpenID Connect identity provider
/// </summary>
public class OidcLoginPlugin(IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    private const string Prefix = "Plugins.ExternalAuth.OpenIdConnect.";

    private static readonly (string Key, string Value)[] Resources = [
        ("Login", "Sign in with your organization account"),
        ("Link.Title", "Connect your account"),
        ("Link.Text",
            "An account with the e-mail {0} already exists in this store. Sign in once with its password to connect it; next time the button is enough."),
        ("Link.SignIn", "Sign in with password"),
        ("Link.Mismatch",
            "You are signed in with a different e-mail address than the one from your identity provider. Nothing was connected."),
        ("Failed.Title", "Sign-in failed"),
        ("Failed.Generic",
            "Signing in with the identity provider did not succeed. Try again or use another sign-in method."),
        ("Failed.AccountInactive",
            "Your account is not active yet. Use the activation link from the e-mail or wait for approval."),
        ("Failed.RegistrationDisabled", "New accounts cannot be registered in this store."),
        ("Failed.LinkedToOtherIdentity", "This account is already connected to another identity provider account."),
        ("Failed.IdentityLinkedToOtherAccount",
            "Your identity provider account is already connected to another account in this store. Nothing was connected."),
        ("Failed.SignedInAsOtherAccount",
            "You are already signed in with another account. Sign out first, then sign in with your identity provider."),
        ("Failed.SessionUnavailable",
            "Signing in with the identity provider is not set up completely. Use another sign-in method, or ask the administrator."),
        ("Failed.EmailNotVerified",
            "Your identity provider has not verified your e-mail address. Verify it there, then sign in again."),
        ("Failed.ProviderChanged",
            "Signing in with the identity provider is paused while the administrator updates the store's connection to it. Try again later.")
    ];

    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return OidcLoginDefaults.ConfigurationUrl;
    }

    public override async Task Install()
    {
        //locales
        foreach (var (key, value) in Resources)
            await pluginTranslateResource.AddOrUpdatePluginTranslateResource(Prefix + key, value);

        await base.Install();
    }

    public override async Task Uninstall()
    {
        //locales
        foreach (var (key, _) in Resources)
            await pluginTranslateResource.DeletePluginTranslationResource(Prefix + key);

        await base.Uninstall();
    }
}
