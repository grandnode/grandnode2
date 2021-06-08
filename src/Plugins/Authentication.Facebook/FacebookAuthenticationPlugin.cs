using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Authentication.Facebook
{
    /// <summary>
    /// Represents method for the authentication with Facebook account
    /// </summary>
    public class FacebookAuthenticationPlugin : BasePlugin, IPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        #endregion

        #region Ctor

        public FacebookAuthenticationPlugin(ISettingService settingService,
            ITranslationService translationService,
            ILanguageService languageService)
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
            return FacebookAuthenticationDefaults.ConfigurationUrl;
        }



        /// <summary>
        /// Install the plugin
        /// </summary>
        public override async Task Install()
        {
            //settings
            await _settingService.SaveSetting(new FacebookExternalAuthSettings());

            //locales
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.Login", "Login using Facebook account");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.ClientKeyIdentifier", "App ID/API Key");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.ClientKeyIdentifier.Hint", "Enter your app ID/API key here. You can find it on your FaceBook application page.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.ClientSecret", "App Secret");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.ClientSecret.Hint", "Enter your app secret here. You can find it on your FaceBook application page.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.Failed", "Facebook - Login error");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.Failed.ErrorCode", "Error code");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.Failed.ErrorMessage", "Error message");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Authentication.Facebook.DisplayOrder", "Display order");
            
            await base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override async Task Uninstall()
        {
            //settings
            await _settingService.DeleteSetting<FacebookExternalAuthSettings>();

            //locales
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Authentication.Facebook.Login");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Authentication.Facebook.ClientKeyIdentifier");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Authentication.Facebook.ClientKeyIdentifier.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Authentication.Facebook.ClientSecret");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Authentication.Facebook.ClientSecret.Hint");

            await base.Uninstall();
        }

        #endregion
    }
}
