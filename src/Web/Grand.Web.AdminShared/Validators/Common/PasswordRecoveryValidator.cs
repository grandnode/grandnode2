using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Captcha;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Validators;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Validators.Common;

/// <summary>
///     The request form of a panel's password recovery. Unlike the storefront's, it never says whether
///     an account exists: that is decided by IPanelPasswordRecoveryService, which answers the same way either way.
/// </summary>
public class PasswordRecoveryValidator : BaseGrandValidator<PasswordRecoveryModel>
{
    public PasswordRecoveryValidator(
        IEnumerable<IValidatorConsumer<PasswordRecoveryModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        ITranslationService translationService, CaptchaSettings captchaSettings,
        IHttpContextAccessor contextAccessor, IGoogleReCaptchaValidator googleReCaptchaValidator)
        : base(validators)
    {
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Account.PasswordRecovery.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

        if (captchaSettings.Enabled && captchaSettings.ShowOnPasswordRecoveryPage)
        {
            RuleFor(x => x.Captcha).NotNull()
                .WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}
