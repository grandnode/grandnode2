using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Models.Customer;

namespace Grand.Web.Validators.Customer
{
    public class PasswordRecoveryValidator : BaseGrandValidator<PasswordRecoveryModel>
    {
        public PasswordRecoveryValidator(
            IEnumerable<IValidatorConsumer<PasswordRecoveryModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Account.PasswordRecovery.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
        }}
}