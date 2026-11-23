using FluentValidation;
using Grand.Domain.Common;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Customer;
using Grand.Web.Common.Localization;

namespace Grand.Web.Validators.Customer;

public class PasswordRecoveryValidator : BaseGrandValidator<PasswordRecoveryModel>
{
    public PasswordRecoveryValidator(
        IEnumerable<IValidatorConsumer<PasswordRecoveryModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        CaptchaSettings captchaSettings,
        IHttpContextAccessor contextAccessor, IGoogleReCaptchaValidator googleReCaptchaValidator,
        IRequestTranslationService translationService)
        : base(validators)
    {
        //nothing about the account behind the address: the page answers the same whether or not it has one
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Account.PasswordRecovery.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
        if (captchaSettings.Enabled && captchaSettings.ShowOnPasswordRecoveryPage)
        {
            RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}