using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Customer;

namespace Grand.Web.Validators.Customer;

public class ChangePasswordValidator : BaseGrandValidator<ChangePasswordModel>
{
    public ChangePasswordValidator(
        IEnumerable<IValidatorConsumer<ChangePasswordModel>> validators,
        ICustomerService customerService, IWorkContext workContext, IGroupService groupService,
        IEncryptionService encryptionService, ICustomerHistoryPasswordService customerHistoryPasswordService,
        ICustomerManagerService customerManagerService,
        ITranslationService translationService, CustomerSettings customerSettings)
        : base(validators)
    {
        RuleFor(x => x.OldPassword).NotEmpty()
            .WithMessage(translationService.GetResource("Account.ChangePassword.Fields.OldPassword.Required"));
        RuleFor(x => x.NewPassword).NotEmpty()
            .WithMessage(translationService.GetResource("Account.ChangePassword.Fields.NewPassword.Required"));

        if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
            RuleFor(x => x.NewPassword).Matches(customerSettings.PasswordRegularExpression).WithMessage(
                string.Format(
                    translationService.GetResource("Account.ChangePassword.Fields.NewPassword.Validation")));

        RuleFor(x => x.ConfirmNewPassword).NotEmpty()
            .WithMessage(
                translationService.GetResource("Account.ChangePassword.Fields.ConfirmNewPassword.Required"));
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage(
            translationService.GetResource("Account.ChangePassword.Fields.NewPassword.EnteredPasswordsDoNotMatch"));

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customer = await customerService.GetCustomerById(workContext.CurrentCustomer.Id);

            switch (customer)
            {
                case null:
                    context.AddFailure(
                        translationService.GetResource("Account.ChangePassword.Errors.EmailIsNotProvided"));
                    break;
                case { Deleted: true }:
                    context.AddFailure(
                        translationService.GetResource("Account.ChangePassword.Errors.Deleted"));
                    break;
                case { Active: false }:
                    context.AddFailure(
                        translationService.GetResource("Account.ChangePassword.Errors.NotActive"));
                    break;
                case { CannotLoginUntilDateUtc: not null }
                    when customer.CannotLoginUntilDateUtc.Value > DateTime.UtcNow:
                    context.AddFailure(
                        translationService.GetResource("Account.ChangePassword.Errors.LockedOut"));
                    break;
                case not null when !await groupService.IsRegistered(customer):
                    context.AddFailure(
                        translationService.GetResource(
                            "Account.ChangePassword.Errors.NotRegistered"));
                    break;
            }

            if (customer is not null)
            {
                var oldPwd = customer.PasswordFormatId switch {
                    PasswordFormat.Encrypted => encryptionService.EncryptText(x.OldPassword,
                        customer.PasswordSalt),
                    PasswordFormat.Hashed => encryptionService.CreatePasswordHash(x.OldPassword,
                        customer.PasswordSalt, customerSettings.HashedPasswordFormat),
                    _ => x.OldPassword
                };
                if (oldPwd != customer.Password)
                    context.AddFailure(
                        translationService.GetResource("Account.ChangePassword.Errors.OldPasswordDoesntMatch"));
            }

            if (customer is not null && customerSettings.UnduplicatedPasswordsNumber > 0)
            {
                //get some of previous passwords
                var previousPasswords = await customerHistoryPasswordService.GetPasswords(customer.Id,
                    customerSettings.UnduplicatedPasswordsNumber);
                var newPasswordMatchesWithPrevious = previousPasswords.Any(password =>
                    customerManagerService.PasswordMatch(customerSettings.DefaultPasswordFormat, password.Password,
                        x.NewPassword, password.PasswordSalt));
                if (newPasswordMatchesWithPrevious)
                    context.AddFailure(
                        translationService.GetResource("Account.ChangePassword.Errors.PasswordMatchesWithPrevious"));
            }
        });
    }
}