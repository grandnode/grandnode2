using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Authentication.Google
{
    /// <summary>
    /// Represents method for the authentication with google account
    /// </summary>
    public class GoogleAuthenticationPlugin : BasePlugin, IPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        #endregion

        #region Ctor

        public GoogleAuthenticationPlugin(ISettingService settingService, ITranslationService translationService, ILanguageService languageService)
        {
            _settingService = settingService;
            _translationService = translationService;
            _languageService = languageService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string ConfigurationUrl()
        {
            return GoogleAuthenticationDefaults.ConfigurationUrl;
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override async Task Install()
        {
            //settings
            await _settingService.SaveSetting(new GoogleExternalAuthSettings());

            //locales
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.Description", "<h4>Google Developers Console</h4><ul><li>Go to the <a href=\"https://console.developers.google.com/\" target=\"_blank\">Google Developers Console</a> and log in with your Google Developer Account</li><li>Select \"Create Project\"</li><li>Go to APIs & Auth -> Credentials in the left-hand navigation panel</li><li>Select \"Create new Client ID\" in the OAuth Panel</li><li>In the creation panel:<ul><li>Select \"Web application\" as Application Type</li><li>Set \"Authorized JavaScript origins\" to the URL of your store (http://www.yourStore.com)</li><li>Set \"Authorized redirect URI\" to URL of login callback (http://www.yourStore.com/signin-google)</li></ul></li><li>Then go to APIs & Auth -> Consent Screen and fill out</li><li>Now get your API key (Client ID and Client Secret) and configure your store</li><li>Please remember to restart the application after changes.</li></ul><p>For more details, read the Google docs: <a href=\"https://developers.google.com/accounts/docs/OAuth2\">Using OAuth 2.0 to Access Google APIs</a>.</p>");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.Login", "Login using Google account");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.ClientKeyIdentifier", "Client ID");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.ClientSecret", "Client Secret");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.Title", "<h4>Configuring Google OAuth2</h4>");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Externalauth.Google.Failed", "Failed authentication");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Externalauth.Google.Failed.Errormessage", "Error message: ");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Externalauth.Google.DisplayOrder", "Display order");

            await base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override async Task Uninstall()
        {
            //settings
            await _settingService.DeleteSetting<GoogleExternalAuthSettings>();

            //locales
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.Description");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.Login");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.ClientKeyIdentifier");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.ClientSecret");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.Title");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.ExternalAuth.Google.DisplayOrder");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Externalauth.Google.Failed");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Externalauth.Google.Failed.Errormessage");

            await base.Uninstall();
        }

        #endregion
    }
}
