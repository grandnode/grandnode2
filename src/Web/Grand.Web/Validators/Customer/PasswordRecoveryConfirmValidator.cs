using FluentValidation;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Customer;
using System.Collections.Generic;

namespace Grand.Web.Validators.Customer
{
    public class PasswordRecoveryConfirmValidator : BaseGrandValidator<PasswordRecoveryConfirmModel>
    {
        public PasswordRecoveryConfirmValidator(
            IEnumerable<IValidatorConsumer<PasswordRecoveryConfirmModel>> validators,
            ITranslationService translationService, CustomerSettings customerSettings)
            : base(validators)
        {
            RuleFor(x => x.NewPassword).NotEmpty().WithMessage(translationService.GetResource("Account.PasswordRecovery.NewPassword.Required"));

            if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
                RuleFor(x => x.NewPassword).Matches(customerSettings.PasswordRegularExpression).WithMessage(string.Format(translationService.GetResource("Account.ChangePassword.Fields.NewPassword.Validation")));

            RuleFor(x => x.ConfirmNewPassword).NotEmpty().WithMessage(translationService.GetResource("Account.PasswordRecovery.ConfirmNewPassword.Required"));
            RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage(translationService.GetResource("Account.PasswordRecovery.NewPassword.EnteredPasswordsDoNotMatch"));
        }
    }
}