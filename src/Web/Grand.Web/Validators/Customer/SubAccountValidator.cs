using FluentValidation;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Customer;
using System.Collections.Generic;

namespace Grand.Web.Validators.Customer
{
    public class SubAccountValidator : BaseGrandValidator<SubAccountModel>
    {
        public SubAccountValidator(
            IEnumerable<IValidatorConsumer<SubAccountModel>> validators,
            ITranslationService translationService,
            CustomerSettings customerSettings)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

            RuleFor(x => x.FirstName).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.FirstName.Required"));
            RuleFor(x => x.LastName).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.LastName.Required"));

            RuleFor(x => x.Password).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Password.Required"))
                .When(subaccount => (string.IsNullOrEmpty(subaccount.Id)) || (!string.IsNullOrEmpty(subaccount.Id) && !string.IsNullOrEmpty(subaccount.Password)));

            if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
                RuleFor(x => x.Password).Matches(customerSettings.PasswordRegularExpression).WithMessage(string.Format(translationService.GetResource("Account.Fields.Password.Validation")))
                    .When(subaccount => (string.IsNullOrEmpty(subaccount.Id)) || (!string.IsNullOrEmpty(subaccount.Id) && !string.IsNullOrEmpty(subaccount.Password)));

        }
    }
}