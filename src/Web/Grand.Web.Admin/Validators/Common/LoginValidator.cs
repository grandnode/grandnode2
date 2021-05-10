using FluentValidation;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Common;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Common
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
                //login by email
                RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Account.Login.Fields.Email.Required"));
                RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            }
        }
    }
}