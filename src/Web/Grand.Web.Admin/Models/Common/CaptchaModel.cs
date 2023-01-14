using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Common;

public record CaptchaModel : ICaptchaValidModel
{
    public string ReCaptchaChallengeField { get; set; }
    public string ReCaptchaResponseField { get; set; }
    public string ReCaptchaResponseValue { get; set; }
    public string ReCaptchaResponse { get; set; }
}