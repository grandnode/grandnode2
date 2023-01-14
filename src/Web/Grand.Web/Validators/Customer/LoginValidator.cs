using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Customers;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Common;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Customer
{
    public class LoginValidator : BaseGrandValidator<LoginModel>
    {
        public LoginValidator(
            IEnumerable<IValidatorConsumer<LoginModel>> validators,
            IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
            ITranslationService translationService, CustomerSettings customerSettings, CaptchaSettings captchaSettings,
            IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator)
            : base(validators)
        {
            if (!customerSettings.UsernamesEnabled)
            {
                RuleFor(x => x.Email).NotEmpty()
                    .WithMessage(translationService.GetResource("Account.Login.Fields.Email.Required"));
                RuleFor(x => x.Password).NotEmpty()
                    .WithMessage(translationService.GetResource("Account.Login.Fields.Password.Required"));
                RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            }
            else
            {
                RuleFor(x => x.Username).NotEmpty()
                    .WithMessage(translationService.GetResource("Account.Login.Fields.UserName.Required"));
                RuleFor(x => x.Password).NotEmpty()
                    .WithMessage(translationService.GetResource("Account.Login.Fields.Password.Required"));
            }

            if (captchaSettings.Enabled && captchaSettings.ShowOnLoginPage)
            {
                RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));;
                RuleFor(x => x.Captcha).SetValidator(new CaptchaValidator(validatorsCaptcha,contextAccessor, googleReCaptchaValidator));
            }
        }
    }
}