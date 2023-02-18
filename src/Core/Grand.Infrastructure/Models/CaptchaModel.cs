namespace Grand.Infrastructure.Models;

public record CaptchaModel : ICaptchaValidModel
{
    public string ReCaptchaChallengeField { get; set; }
    public string ReCaptchaResponseField { get; set; }
    public string ReCaptchaResponseValue { get; set; }
    public string ReCaptchaResponse { get; set; }
}