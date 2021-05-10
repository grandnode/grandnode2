using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Validators;
using System.Collections.Generic;

namespace Grand.Api.Validators.Catalog
{
    public class ProductSpecificationAttributeValidator : BaseGrandValidator<ProductSpecificationAttributeDto>
    {
        public ProductSpecificationAttributeValidator(
            IEnumerable<IValidatorConsumer<ProductSpecificationAttributeDto>> validators,
            ITranslationService translationService, ISpecificationAttributeService specificationAttributeService)
            : base(validators)
        {
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                var specification = await specificationAttributeService.GetSpecificationAttributeById(x.SpecificationAttributeId);
                if (specification == null)
                    return false;
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductSpecificationAttribute.Fields.SpecificationAttributeId.NotExists"));

            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.SpecificationAttributeOptionId))
                {
                    var sa = await specificationAttributeService.GetSpecificationAttributeByOptionId(x.SpecificationAttributeOptionId);
                    if (sa == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductSpecificationAttribute.Fields.SpecificationAttributeOptionId.NotExists"));
        }
    }
}
