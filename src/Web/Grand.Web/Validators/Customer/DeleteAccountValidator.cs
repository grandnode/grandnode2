using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Models.Customer;

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