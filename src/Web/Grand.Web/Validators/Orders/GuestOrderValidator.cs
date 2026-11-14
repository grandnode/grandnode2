using FluentValidation;
using Grand.Domain.Common;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Captcha;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Orders;

namespace Grand.Web.Validators.Orders;

public class GuestOrderValidator : BaseGrandValidator<GuestOrderModel>
{
    public GuestOrderValidator(
        IEnumerable<IValidatorConsumer<GuestOrderModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        CaptchaSettings captchaSettings,
        IHttpContextAccessor httpContextAccessor, IGoogleReCaptchaValidator googleReCaptchaValidator,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.OrderCode).NotEmpty()
            .WithMessage(translationService.GetResource("Order.Guest.OrderCode.Required"));
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Order.Guest.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

        if (captchaSettings.Enabled && captchaSettings.ShowOnGuestOrderPage)
        {
            RuleFor(x => x.Captcha).NotNull()
                .WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, httpContextAccessor, googleReCaptchaValidator));
        }
    }
}
