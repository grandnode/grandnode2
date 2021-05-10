using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Validators;
using System.Collections.Generic;

namespace Grand.Api.Validators.Catalog
{
    public class ProductAttributeMappingValidator : BaseGrandValidator<ProductAttributeMappingDto>
    {
        public ProductAttributeMappingValidator(
            IEnumerable<IValidatorConsumer<ProductAttributeMappingDto>> validators,
            ITranslationService translationService, IProductAttributeService productAttributeService)
            : base(validators)
        {
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                var productattribute = await productAttributeService.GetProductAttributeById(x.ProductAttributeId);
                if (productattribute == null)
                    return false;
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductAttributeMapping.Fields.ProductAttributeId.NotExists"));
        }
    }
}
