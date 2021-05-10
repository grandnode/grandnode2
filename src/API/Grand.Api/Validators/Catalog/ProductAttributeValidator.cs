using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Validators;
using System.Collections.Generic;

namespace Grand.Api.Validators.Catalog
{
    public class ProductAttributeValidator : BaseGrandValidator<ProductAttributeDto>
    {
        public ProductAttributeValidator(IEnumerable<IValidatorConsumer<ProductAttributeDto>> validators,
            ITranslationService translationService, IProductAttributeService productAttributeService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Api.Catalog.ProductAttribute.Fields.Name.Required"));
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.Id))
                {
                    var pa = await productAttributeService.GetProductAttributeById(x.Id);
                    if (pa == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductAttribute.Fields.Id.NotExists"));
            RuleFor(x => x).Must((x, context) =>
            {
                foreach (var item in x.PredefinedProductAttributeValues)
                {
                    if (string.IsNullOrEmpty(item.Name))
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.PredefinedProductAttributeValue.Fields.Name.Required"));
        }
    }
}
