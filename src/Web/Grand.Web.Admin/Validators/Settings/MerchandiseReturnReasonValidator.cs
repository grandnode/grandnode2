using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Validators.Settings
{
    public class MerchandiseReturnReasonValidator : BaseGrandValidator<MerchandiseReturnReasonModel>
    {
        public MerchandiseReturnReasonValidator(
            IEnumerable<IValidatorConsumer<MerchandiseReturnReasonModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Name.Required"));
        }
    }
}