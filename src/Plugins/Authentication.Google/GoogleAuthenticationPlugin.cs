using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Authentication.Google;

/// <summary>
///     Represents method for the authentication with google account
/// </summary>
public class GoogleAuthenticationPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{

    #region Methods

    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return GoogleAuthenticationDefaults.ConfigurationUrl;
    }

    /// <summary>
    ///     Install the plugin
    /// </summary>
    public override async Task Install()
    {
        //settings
        await settingService.SaveSetting(new GoogleExternalAuthSettings());

        //locales
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.ExternalAuth.Google.Description",
            "<h4>Google Developers Console</h4><ul><li>Go to the <a href=\"https://console.developers.google.com/\" target=\"_blank\">Google Developers Console</a> and log in with your Google Developer Account</li><li>Select \"Create Project\"</li><li>Go to APIs & Auth -> Credentials in the left-hand navigation panel</li><li>Select \"Create new Client ID\" in the OAuth Panel</li><li>In the creation panel:<ul><li>Select \"Web application\" as Application Type</li><li>Set \"Authorized JavaScript origins\" to the URL of your store (http://www.yourStore.com)</li><li>Set \"Authorized redirect URI\" to URL of login callback (http://www.yourStore.com/signin-google)</li></ul></li><li>Then go to APIs & Auth -> Consent Screen and fill out</li><li>Now get your API key (Client ID and Client Secret) and configure your store</li><li>Please remember to restart the application after changes.</li></ul><p>For more details, read the Google docs: <a href=\"https://developers.google.com/accounts/docs/OAuth2\">Using OAuth 2.0 to Access Google APIs</a>.</p>");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.ExternalAuth.Google.Login", "Login using Google account");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.ExternalAuth.Google.ClientKeyIdentifier", "Client ID");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.ExternalAuth.Google.ClientSecret", "Client Secret");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.ExternalAuth.Google.Title", "<h4>Configuring Google OAuth2</h4>");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.ExternalAuth.Google.Failed", "Failed authentication");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.ExternalAuth.Google.Failed.Errormessage", "Error message: ");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.ExternalAuth.Google.DisplayOrder", "Display order");

        await base.Install();
    }

    /// <summary>
    ///     Uninstall the plugin
    /// </summary>
    public override async Task Uninstall()
    {
        //settings
        await settingService.DeleteSetting<GoogleExternalAuthSettings>();

        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.ExternalAuth.Google.Description");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.ExternalAuth.Google.Login");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.ExternalAuth.Google.ClientKeyIdentifier");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.ExternalAuth.Google.ClientSecret");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.ExternalAuth.Google.Title");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.ExternalAuth.Google.DisplayOrder");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.ExternalAuth.Google.Failed");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.ExternalAuth.Google.Failed.Errormessage");

        await base.Uninstall();
    }

    #endregion
}