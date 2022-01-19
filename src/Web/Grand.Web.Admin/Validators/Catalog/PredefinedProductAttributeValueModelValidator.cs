using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog
{
    public class PredefinedProductAttributeValueModelValidator : BaseGrandValidator<PredefinedProductAttributeValueModel>
    {
        public PredefinedProductAttributeValueModelValidator(
            IEnumerable<IValidatorConsumer<PredefinedProductAttributeValueModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Name.Required"));
        }
    }
}