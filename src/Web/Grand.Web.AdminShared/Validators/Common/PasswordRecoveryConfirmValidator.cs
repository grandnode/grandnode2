using FluentValidation;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Common;

/// <summary>
///     The new password of a panel's password recovery. The token, the account and the password history are
///     checked by IPanelPasswordRecoveryService when the password is changed.
/// </summary>
public class PasswordRecoveryConfirmValidator : BaseGrandValidator<PasswordRecoveryConfirmModel>
{
    public PasswordRecoveryConfirmValidator(
        IEnumerable<IValidatorConsumer<PasswordRecoveryConfirmModel>> validators,
        IRequestTranslationService translationService, CustomerSettings customerSettings)
        : base(validators)
    {
        RuleFor(x => x.NewPassword).NotEmpty()
            .WithMessage(translationService.GetResource("Account.PasswordRecovery.NewPassword.Required"));

        if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
            RuleFor(x => x.NewPassword).Matches(customerSettings.PasswordRegularExpression)
                .WithMessage(translationService.GetResource("Account.ChangePassword.Fields.NewPassword.Validation"));

        RuleFor(x => x.ConfirmNewPassword).NotEmpty()
            .WithMessage(translationService.GetResource("Account.PasswordRecovery.ConfirmNewPassword.Required"));
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword)
            .WithMessage(translationService.GetResource(
                "Account.PasswordRecovery.NewPassword.EnteredPasswordsDoNotMatch"));
    }
}
