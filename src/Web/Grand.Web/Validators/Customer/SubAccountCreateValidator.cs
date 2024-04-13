using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Customer;

namespace Grand.Web.Validators.Customer;

public class SubAccountCreateValidator : BaseGrandValidator<SubAccountCreateModel>
{
    public SubAccountCreateValidator(
        IEnumerable<IValidatorConsumer<SubAccountCreateModel>> validators,
        ICustomerService customerService,
        ITranslationService translationService,
        CustomerSettings customerSettings)
        : base(validators)
    {
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Fields.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

        RuleFor(x => x.FirstName).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Fields.FirstName.Required"));
        RuleFor(x => x.LastName).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Fields.LastName.Required"));

        RuleFor(x => x.Password).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Fields.Password.Required"));

        if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
            RuleFor(x => x.Password).Matches(customerSettings.PasswordRegularExpression)
                .WithMessage(string.Format(translationService.GetResource("Account.Fields.Password.Validation")));

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customer = await customerService.GetCustomerByEmail(x.Email);
            if (customer != null)
                context.AddFailure(
                    translationService.GetResource("Account.EmailUsernameErrors.EmailAlreadyExists"));
        });
    }
}