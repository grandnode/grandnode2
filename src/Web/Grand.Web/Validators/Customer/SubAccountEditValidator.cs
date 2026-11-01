using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Extensions;
using Grand.Web.Models.Customer;
using Grand.Web.Common.Localization;

namespace Grand.Web.Validators.Customer;

public class SubAccountEditValidator : BaseGrandValidator<SubAccountEditModel>
{
    public SubAccountEditValidator(
        IEnumerable<IValidatorConsumer<SubAccountEditModel>> validators,
        ICustomerService customerService, IGroupService groupService,
        IRequestTranslationService translationService,
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

        RuleFor(x => x.Role).IsInEnum()
            .WithMessage(translationService.GetResource("Account.SubAccounts.Fields.Role.Invalid"));

        //empty means no limit; anything else must read as a non-negative number in the current culture
        RuleFor(x => x.OrderApprovalLimit)
            .Must(text => OrderApprovalLimitText.TryParse(text, out _))
            .WithMessage(translationService.GetResource("Account.SubAccounts.Fields.OrderApprovalLimit.Invalid"));

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customer = await customerService.GetCustomerById(x.Id);

            //ownership first: a missing id, a deleted account and someone else's account get the same answer
            //(the edit page and the command treat them alike), so it reveals neither ids nor their state
            if (customer == null || customer.Deleted ||
                customer.OwnerId != contextAccessor.WorkContext.CurrentCustomer.Id)
            {
                context.AddFailure(translationService.GetResource("Account.SubAccounts.NotOwner"));
                return;
            }

            switch (customer)
            {
                case not null when !await groupService.IsRegistered(customer):
                    context.AddFailure(
                        translationService.GetResource("Account.Login.WrongCredentials.NotRegistered"));
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