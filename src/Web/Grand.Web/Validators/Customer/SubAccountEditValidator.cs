using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
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
        IWorkContext workContext,
        CustomerSettings customerSettings)
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

            switch (customer)
            {
                case null:
                    context.AddFailure(
                        translationService.GetResource("Account.NoExists"));
                    break;
                case { Deleted: true }:
                    context.AddFailure(
                        translationService.GetResource("Account.DeleteAccount"));
                    break;
                case not null when !await groupService.IsRegistered(customer):
                    context.AddFailure(
                        translationService.GetResource(
                            "Account is not registered"));
                    break;
            }

            if (customer != null && customer.OwnerId != workContext.CurrentCustomer.Id)
                context.AddFailure("You are not owner of this account");

            if (customer != null && customer.Email != x.Email.ToLower() && customerSettings.AllowUsersToChangeEmail)
            {
                if (!CommonHelper.IsValidEmail(x.Email))
                    context.AddFailure(
                        translationService.GetResource("Account.EmailUsernameErrors.NewEmailIsNotValid"));

                if (x.Email.Length > 100)
                    context.AddFailure(translationService.GetResource("Account.EmailUsernameErrors.EmailTooLong"));

                var customer2 = await customerService.GetCustomerByEmail(x.Email);
                if (customer2 != null && customer.Id != customer2.Id)
                    context.AddFailure(
                        translationService.GetResource("Account.EmailUsernameErrors.EmailAlreadyExists"));
            }
        });
    }
}