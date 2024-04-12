using Grand.Infrastructure.TypeConverters.JsonConverters;
using Grand.SharedKernel.Attributes;

namespace Grand.Infrastructure.Models;

[InterfaceConverter(typeof(InterfaceConverter<ICaptchaValidModel, CaptchaModel>))]
public interface ICaptchaValidModel
{
    public string ReCaptchaChallengeField { get; set; }
    public string ReCaptchaResponseField { get; set; }
    public string ReCaptchaResponseValue { get; set; }
    public string ReCaptchaResponse { get; set; }
}