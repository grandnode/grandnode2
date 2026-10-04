namespace Grand.SharedKernel.Captcha;
public interface IGoogleReCaptchaValidator
{
    Task<GoogleReCaptchaResponse> Validate(string response);
}
