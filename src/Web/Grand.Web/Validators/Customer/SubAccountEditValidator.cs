using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Extensions;
using Grand.Web.Models.Customer;

namespace Grand.Web.Validators.Customer;

public class SubAccountEditValidator : BaseGrandValidator<SubAccountEditModel>
{
    public SubAccountEditValidator(
        IEnumerable<IValidatorConsumer<SubAccountEditModel>> validators,
        ICustomerService customerService, IGroupService groupService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        CustomerSettings customerSettings,
        CustomerConfig customerConfig)
        : base(validators)
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

        RuleFor(x => x.FirstName).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Fields.FirstName.Required"));
        RuleFor(x => x.LastName).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Fields.LastName.Required"));

        if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
            RuleFor(x => x.Password).Matches(customerSettings.PasswordRegularExpression)
                .WithMessage(string.Format(translationService.GetResource("Account.Fields.Password.Validation")))
                .When(subAccount => !string.IsNullOrEmpty(subAccount.Password));

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customer = await customerService.GetCustomerById(x.Id);

            //ownership first: a missing id, a deleted account and someone else's account get the same answer
            if (customer == null || customer.Deleted ||
                customer.OwnerId != contextAccessor.WorkContext.CurrentCustomer.Id)
            {
                context.AddFailure("You are not owner of this account");
                return;
            }

            if (!await groupService.IsRegistered(customer))
            {
                context.AddFailure(translationService.GetResource("Account is not registered"));
                return;
            }

            //an empty email is already reported by the NotEmpty rule
            if (string.IsNullOrWhiteSpace(x.Email))
                return;

            //the same address the command stores, checked where the account lives
            var email = x.Email.Trim().ToLowerInvariant();
            var sameAddress = string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase);
            if (customer.Email != email && (sameAddress || customerSettings.AllowUsersToChangeEmail))
            {
                if (!CommonHelper.IsValidEmail(email))
                    context.AddFailure(
                        translationService.GetResource("Account.EmailUsernameErrors.NewEmailIsNotValid"));

                if (email.Length > 100)
                    context.AddFailure(translationService.GetResource("Account.EmailUsernameErrors.EmailTooLong"));

                var storeId = customerConfig.RegisterCustomersPerStore ? customer.StoreId : "";
                var customer2 = await customerService.GetCustomerByEmail(email, storeId);
                if (customer2 != null && customer.Id != customer2.Id)
                    context.AddFailure(
                        translationService.GetResource("Account.EmailUsernameErrors.EmailAlreadyExists"));
            }
        });
    }
}