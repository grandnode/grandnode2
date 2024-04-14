using FluentValidation;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Admin.Validators.Common;

public class LoginValidator : BaseGrandValidator<LoginModel>
{
    public LoginValidator(
        IEnumerable<IValidatorConsumer<LoginModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        ICustomerService customerService, IGroupService groupService, IEncryptionService encryptionService,
        ITranslationService translationService, CustomerSettings customerSettings, CaptchaSettings captchaSettings,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator)
        : base(validators)
    {
        if (!customerSettings.UsernamesEnabled)
        {
            RuleFor(x => x.Email).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Login.Fields.Email.Required"));

            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
        }
        else
        {
            RuleFor(x => x.Username).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Login.Fields.UserName.Required"));
        }

        RuleFor(x => x.Password).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Login.Fields.Password.Required"));

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customer = customerSettings.UsernamesEnabled
                ? await customerService.GetCustomerByUsername(x.Username)
                : await customerService.GetCustomerByEmail(x.Email);

            switch (customer)
            {
                case null:
                    context.AddFailure(
                        translationService.GetResource("Account.Login.WrongCredentials.CustomerNotExist"));
                    break;
                case { Deleted: true }:
                    context.AddFailure(translationService.GetResource("Account.Login.WrongCredentials.Deleted"));
                    break;
                case { Active: false }:
                    context.AddFailure(translationService.GetResource("Account.Login.WrongCredentials.NotActive"));
                    break;
                case not null when !await groupService.IsRegistered(customer):
                    context.AddFailure(
                        translationService.GetResource("Account.Login.WrongCredentials.NotRegistered"));
                    break;
                case { CannotLoginUntilDateUtc: not null }
                    when customer.CannotLoginUntilDateUtc.Value > DateTime.UtcNow:
                    context.AddFailure(translationService.GetResource("Account.Login.WrongCredentials.LockedOut"));
                    break;
                case not null:
                {
                    var pwd = customer.PasswordFormatId switch {
                        PasswordFormat.Clear => x.Password,
                        PasswordFormat.Encrypted =>
                            encryptionService.EncryptText(x.Password, customer.PasswordSalt),
                        PasswordFormat.Hashed => encryptionService.CreatePasswordHash(x.Password,
                            customer.PasswordSalt,
                            customerSettings.HashedPasswordFormat),
                        _ => throw new Exception("PasswordFormat not supported")
                    };
                    var isValid = pwd == customer.Password;
                    if (!isValid)
                    {
                        context.AddFailure(translationService.GetResource("Account.Login.WrongCredentials"));
                        await contextAccessor.HttpContext!.RequestServices.GetRequiredService<IMediator>()
                            .Publish(new CustomerLoginFailedEvent(customer), _);
                    }

                    break;
                }
            }
        });

        if (captchaSettings.Enabled && captchaSettings.ShowOnLoginPage)
        {
            RuleFor(x => x.Captcha).NotNull()
                .WithMessage(translationService.GetResource("Account.Captcha.Required"));

            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }
    }
}