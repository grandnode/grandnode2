using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class PredefinedProductAttributeValueModelValidator : BaseGrandValidator<PredefinedProductAttributeValueModel>
{
    public PredefinedProductAttributeValueModelValidator(
        IEnumerable<IValidatorConsumer<PredefinedProductAttributeValueModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Catalog.Attributes.ProductAttributes.PredefinedValues.Fields.Name.Required"));
    }
}