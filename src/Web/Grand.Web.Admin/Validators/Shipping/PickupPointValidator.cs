using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Validators.Shipping
{
    public class PickupPointValidator : BaseGrandValidator<PickupPointModel>
    {
        public PickupPointValidator(
            IEnumerable<IValidatorConsumer<PickupPointModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Fields.Name.Required"));
        }
    }
}