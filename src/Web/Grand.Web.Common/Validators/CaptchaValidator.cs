using FluentValidation;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Grand.Web.Common.Validators;

public class CaptchaValidator : BaseGrandValidator<ICaptchaValidModel>
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly GoogleReCaptchaValidator _googleReCaptchaValidator;

    public CaptchaValidator(IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validators,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator
    ) : base(validators)
    {
        _contextAccessor = contextAccessor;
        _googleReCaptchaValidator = googleReCaptchaValidator;

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var result = await ValidateCaptcha(x);
            if (!result.isValid) context.AddFailure(result.error);
        });
    }

    private async Task<(bool isValid, string error)> ValidateCaptcha(ICaptchaValidModel captcha)
    {
        var isValid = false;
        string captchaChallengeValue;
        string captchaResponseValue;
        string gCaptchaResponseValue;
        if (_contextAccessor.HttpContext!.Request.HasJsonContentType())
        {
            captchaChallengeValue = captcha.ReCaptchaChallengeField;
            captchaResponseValue = captcha.ReCaptchaResponseField;
            gCaptchaResponseValue = captcha.ReCaptchaResponseValue;
            if (string.IsNullOrEmpty(gCaptchaResponseValue))
                gCaptchaResponseValue = captcha.ReCaptchaResponse;
        }
        else
        {
            var form = await _contextAccessor.HttpContext!.Request.ReadFormAsync();
            captchaChallengeValue = form[ChallengeFieldKey];
            captchaResponseValue = form[ResponseFieldKey];
            gCaptchaResponseValue = string.Empty;
            foreach (var item in form.Keys)
                if (item.Contains(GResponseFieldKeyV3))
                    gCaptchaResponseValue = form[item];

            if (string.IsNullOrEmpty(gCaptchaResponseValue))
                gCaptchaResponseValue = form[GResponseFieldKeyV2];
        }

        if ((StringValues.IsNullOrEmpty(captchaChallengeValue) ||
             StringValues.IsNullOrEmpty(captchaResponseValue)) &&
            string.IsNullOrEmpty(gCaptchaResponseValue)) return (false, "Captcha response value is null");
        //Captcha validate request
        var recaptchaResponse = await _googleReCaptchaValidator.Validate(
            !StringValues.IsNullOrEmpty(captchaResponseValue)
                ? captchaResponseValue
                : gCaptchaResponseValue);
        isValid = recaptchaResponse.Success;

        return isValid ? (true, string.Empty) : (false, string.Join(',', recaptchaResponse.ErrorCodes));
    }

    #region Constants

    private const string ChallengeFieldKey = "recaptcha_challenge_field";
    private const string ResponseFieldKey = "recaptcha_response_field";
    private const string GResponseFieldKeyV3 = "g-recaptcha-response-value";
    private const string GResponseFieldKeyV2 = "g-recaptcha-response";

    #endregion
}