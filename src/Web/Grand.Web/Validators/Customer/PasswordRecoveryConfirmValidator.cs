using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Customer;

namespace Grand.Web.Validators.Customer;

public class PasswordRecoveryConfirmValidator : BaseGrandValidator<PasswordRecoveryConfirmModel>
{
    public PasswordRecoveryConfirmValidator(
        IEnumerable<IValidatorConsumer<PasswordRecoveryConfirmModel>> validators,
        ICustomerService customerService, IGroupService groupService,
        ICustomerManagerService customerManagerService,
        ICustomerHistoryPasswordService customerHistoryPasswordService,
        ITranslationService translationService, CustomerSettings customerSettings)
        : base(validators)
    {
        RuleFor(x => x.NewPassword).NotEmpty()
            .WithMessage(translationService.GetResource("Account.PasswordRecovery.NewPassword.Required"));

        if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
            RuleFor(x => x.NewPassword).Matches(customerSettings.PasswordRegularExpression).WithMessage(
                string.Format(
                    translationService.GetResource("Account.ChangePassword.Fields.NewPassword.Validation")));

        RuleFor(x => x.ConfirmNewPassword).NotEmpty()
            .WithMessage(translationService.GetResource("Account.PasswordRecovery.ConfirmNewPassword.Required"));
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage(
            translationService.GetResource("Account.PasswordRecovery.NewPassword.EnteredPasswordsDoNotMatch"));

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
                case not null when !await groupService.IsRegistered(customer):
                    context.AddFailure(translationService.GetResource("Account.PasswordRecovery.NotRegistered"));
                    break;
            }

            if (customer is not null && !customer.IsPasswordRecoveryTokenValid(x.Token))
                context.AddFailure(translationService.GetResource("Account.PasswordRecovery.WrongToken"));

            if (customer is not null && customer.IsPasswordRecoveryLinkExpired(customerSettings))
                context.AddFailure(translationService.GetResource("Account.PasswordRecovery.LinkExpired"));

            if (customer is not null && customerSettings.UnduplicatedPasswordsNumber > 0)
            {
                //get some of previous passwords
                var previousPasswords =
                    await customerHistoryPasswordService.GetPasswords(customer.Id,
                        customerSettings.UnduplicatedPasswordsNumber);
                var newPasswordMatchesWithPrevious = previousPasswords.Any(password =>
                    customerManagerService.PasswordMatch(customerSettings.DefaultPasswordFormat, password.Password,
                        x.NewPassword, password.PasswordSalt));
                if (newPasswordMatchesWithPrevious)
                    context.AddFailure(
                        translationService.GetResource("Account.PasswordRecovery.PasswordMatchesWithPrevious"));
            }
        });
    }
}