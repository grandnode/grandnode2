using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Catalog;

public class ProductSpecificationAttributeValidator : BaseGrandValidator<ProductSpecificationAttributeDto>
{
    public ProductSpecificationAttributeValidator(
        IEnumerable<IValidatorConsumer<ProductSpecificationAttributeDto>> validators,
        ITranslationService translationService, ISpecificationAttributeService specificationAttributeService)
        : base(validators)
    {
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            var specification =
                await specificationAttributeService.GetSpecificationAttributeById(x.SpecificationAttributeId);
            return specification != null;
        }).WithMessage(
            translationService.GetResource(
                "Api.Catalog.ProductSpecificationAttribute.Fields.SpecificationAttributeId.NotExists"));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.SpecificationAttributeOptionId))
            {
                var sa = await specificationAttributeService.GetSpecificationAttributeByOptionId(
                    x.SpecificationAttributeOptionId);
                if (sa == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource(
            "Api.Catalog.ProductSpecificationAttribute.Fields.SpecificationAttributeOptionId.NotExists"));
    }
}