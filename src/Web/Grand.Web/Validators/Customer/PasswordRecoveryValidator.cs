using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Customer;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Customer;

public class PasswordRecoveryValidator : BaseGrandValidator<PasswordRecoveryModel>
{
    public PasswordRecoveryValidator(
        IEnumerable<IValidatorConsumer<PasswordRecoveryModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        ICustomerService customerService, CaptchaSettings captchaSettings,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Account.PasswordRecovery.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customer = await customerService.GetCustomerByEmail(x.Email);

            switch (customer)
            {
                case null:
                    context.AddFailure(translationService.GetResource("Account.PasswordRecovery.EmailNotFound"));
                    break;
                case { Deleted: true }:
                    context.AddFailure(translationService.GetResource("Account.PasswordRecovery.Deleted"));
                    break;
                case { Active: false }:
                    context.AddFailure(translationService.GetResource("Account.PasswordRecovery.NotActive"));
                    break;
                case { CannotLoginUntilDateUtc: not null }
                    when customer.CannotLoginUntilDateUtc.Value > DateTime.UtcNow:
                    context.AddFailure(translationService.GetResource("Account.PasswordRecovery.LockedOut"));
                    break;
            }
        });
        if (captchaSettings.Enabled && captchaSettings.ShowOnPasswordRecoveryPage)
        {
            RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}