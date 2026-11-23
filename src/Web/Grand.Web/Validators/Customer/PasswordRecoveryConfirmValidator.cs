using FluentValidation;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Customer;
using Grand.Web.Common.Localization;

namespace Grand.Web.Validators.Customer;

public class PasswordRecoveryConfirmValidator : BaseGrandValidator<PasswordRecoveryConfirmModel>
{
    public PasswordRecoveryConfirmValidator(
        IEnumerable<IValidatorConsumer<PasswordRecoveryConfirmModel>> validators,
        ICustomerService customerService, IGroupService groupService,
        ICustomerManagerService customerManagerService,
        ICustomerHistoryPasswordService customerHistoryPasswordService,
        IRequestTranslationService translationService, CustomerSettings customerSettings,
        IContextAccessor contextAccessor,
        IExternalSignInPolicy externalSignInPolicy)
        : base(validators)
    {
        var storeId = contextAccessor.StoreContext.CurrentStore.Id;

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
            var customer = await customerService.GetCustomerByEmail(x.Email, storeId);

            //an address without an account that may sign in reads as a wrong link, as in the panels, so the page does
            //not tell which addresses have one; so does a link sent before the account had to sign in through the provider
            if (customer is not { Deleted: false, Active: true } ||
                customer.CannotLoginUntilDateUtc > DateTime.UtcNow ||
                !await groupService.IsRegistered(customer) ||
                !customer.IsPasswordRecoveryTokenValid(x.Token) ||
                !(await externalSignInPolicy.PasswordSignIn(customer, SignInSurface.Storefront)).Allowed)
            {
                context.AddFailure(translationService.GetResource("Account.PasswordRecovery.WrongToken"));
                return;
            }

            if (customer.IsPasswordRecoveryLinkExpired(customerSettings))
                context.AddFailure(translationService.GetResource("Account.PasswordRecovery.LinkExpired"));

            if (customerSettings.UnduplicatedPasswordsNumber > 0)
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