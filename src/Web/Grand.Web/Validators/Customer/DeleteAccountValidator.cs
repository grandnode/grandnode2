using FluentValidation;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Customer;
using System.Collections.Generic;

namespace Grand.Web.Validators.Customer
{
    public class DeleteAccountValidator : BaseGrandValidator<DeleteAccountModel>
    {
        public DeleteAccountValidator(
            IEnumerable<IValidatorConsumer<DeleteAccountModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Password).NotEmpty().WithMessage(translationService.GetResource("Account.DeleteAccount.Fields.Password.Required"));
        }}
}