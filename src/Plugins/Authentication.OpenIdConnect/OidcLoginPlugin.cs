using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Authentication.OpenIdConnect;

/// <summary>
///     Sign-in with an OpenID Connect identity provider
/// </summary>
public class OidcLoginPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
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
        ("DisplayOrder", "Display order"),
        ("ClientId", "Client ID"),
        ("ClientSecret", "Client secret"),
        ("ClientSecret.Set", "set"),
        ("ClientSecret.NotSet", "not set"),
        ("Authority", "Identity provider (Authority)"),
        ("Description",
            "<p>The identity provider and the client are configured in the application settings file (appsettings), not here: " +
            "section <code>OidcResourceServer</code> holds the identity provider (Authority), " +
            "section <code>OidcLogin</code> holds the client of this store (ClientId, ClientSecret, Scopes).</p>" +
            "<p>Register this redirect URI at the identity provider for every URL of the store: " +
            "<code>https://&lt;store-url&gt;/signin-oidc</code>.</p>" +
            "<p>Restart the application after changing the settings. Staff accounts connect once with their password; " +
            "use the multi-factor authentication of the identity provider for them.</p>")
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
        //settings
        await settingService.SaveSetting(new OidcLoginSettings());

        //locales
        foreach (var (key, value) in Resources)
            await pluginTranslateResource.AddOrUpdatePluginTranslateResource(Prefix + key, value);

        await base.Install();
    }

    public override async Task Uninstall()
    {
        //settings
        await settingService.DeleteSetting<OidcLoginSettings>();

        //locales
        foreach (var (key, _) in Resources)
            await pluginTranslateResource.DeletePluginTranslationResource(Prefix + key);

        await base.Uninstall();
    }
}
