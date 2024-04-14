using Grand.Business.Core.Interfaces.Common.Localization;

namespace Grand.Web.Common.Security.Captcha;

public static class CaptchaSettingsExtension
{
    public static string GetWrongCaptchaMessage(this CaptchaSettings captchaSettings,
        ITranslationService translationService)
    {
        return captchaSettings.ReCaptchaVersion switch {
            GoogleReCaptchaVersion.V2 => translationService.GetResource("Common.WrongCaptchaV2"),
            GoogleReCaptchaVersion.V3 => translationService.GetResource("Common.WrongCaptchaV3"),
            _ => string.Empty
        };
    }
}