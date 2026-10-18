using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class PredefinedProductAttributeValueModelValidator : BaseGrandValidator<PredefinedProductAttributeValueModel>
{
    public PredefinedProductAttributeValueModelValidator(
        IEnumerable<IValidatorConsumer<PredefinedProductAttributeValueModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Name.Required"));
    }
}