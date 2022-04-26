using Grand.Business.Core.Interfaces.Common.Localization;

namespace Grand.Web.Common.Security.Captcha
{
    public static class CaptchaSettingsExtension
    {
        public static string GetWrongCaptchaMessage(this CaptchaSettings captchaSettings,
            ITranslationService translationService)
        {
            if (captchaSettings.ReCaptchaVersion == GoogleReCaptchaVersion.V2)
                return translationService.GetResource("Common.WrongCaptchaV2");
            if (captchaSettings.ReCaptchaVersion == GoogleReCaptchaVersion.V3)
                return translationService.GetResource("Common.WrongCaptchaV3");
            return string.Empty;
        }
    }
}