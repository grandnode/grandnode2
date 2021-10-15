using FluentValidation;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Customer;
using System.Collections.Generic;

namespace Grand.Web.Validators.Customer
{
    public class LoginValidator : BaseGrandValidator<LoginModel>
    {
        public LoginValidator(
            IEnumerable<IValidatorConsumer<LoginModel>> validators,
            ITranslationService translationService, CustomerSettings customerSettings)
            : base(validators)
        {
            if (!customerSettings.UsernamesEnabled)
            {
                RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Account.Login.Fields.Email.Required"));
                RuleFor(x => x.Password).NotEmpty().WithMessage(translationService.GetResource("Account.Login.Fields.Password.Required"));
                RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            }
            else
            {
                RuleFor(x => x.Username).NotEmpty().WithMessage(translationService.GetResource("Account.Login.Fields.UserName.Required"));
                RuleFor(x => x.Password).NotEmpty().WithMessage(translationService.GetResource("Account.Login.Fields.Password.Required"));
            }
        }
    }
}