using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Authentication.Facebook;

/// <summary>
///     Represents method for the authentication with Facebook account
/// </summary>
public class FacebookAuthenticationPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource) : BasePlugin, IPlugin
{
    #region Methods

    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return FacebookAuthenticationDefaults.ConfigurationUrl;
    }


    /// <summary>
    ///     Install the plugin
    /// </summary>
    public override async Task Install()
    {
        //settings
        await settingService.SaveSetting(new FacebookExternalAuthSettings());

        //locales
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Authentication.Facebook.Login", "Login using Facebook account");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Authentication.Facebook.ClientKeyIdentifier", "App ID/API Key");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Authentication.Facebook.ClientSecret", "App Secret");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Authentication.Facebook.Failed", "Facebook - Login error");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Authentication.Facebook.Failed.ErrorCode", "Error code");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Authentication.Facebook.Failed.ErrorMessage", "Error message");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Authentication.Facebook.DisplayOrder", "Display order");

        await base.Install();
    }

    /// <summary>
    ///     Uninstall the plugin
    /// </summary>
    public override async Task Uninstall()
    {
        //settings
        await settingService.DeleteSetting<FacebookExternalAuthSettings>();

        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Authentication.Facebook.Login");
        await pluginTranslateResource.DeletePluginTranslationResource("Authentication.Facebook.ClientKeyIdentifier");
        await pluginTranslateResource.DeletePluginTranslationResource("Authentication.Facebook.ClientSecret");

        await base.Uninstall();
    }

    #endregion
}