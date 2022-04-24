using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Validators.Shipping
{
    public class ShippingMethodValidator : BaseGrandValidator<ShippingMethodModel>
    {
        public ShippingMethodValidator(
            IEnumerable<IValidatorConsumer<ShippingMethodModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Shipping.Methods.Fields.Name.Required"));
        }
    }
}