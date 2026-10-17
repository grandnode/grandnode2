using FluentValidation;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;

namespace Grand.Module.Api.Validators.Catalog;

public class ProductAttributeMappingValidator : BaseGrandValidator<ProductAttributeMappingDto>
{
    public ProductAttributeMappingValidator(
        IEnumerable<IValidatorConsumer<ProductAttributeMappingDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor, IProductAttributeService productAttributeService)
        : base(validators)
    {
        var languageId = contextAccessor.WorkContext.WorkingLanguage.Id;

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            var productAttribute = await productAttributeService.GetProductAttributeById(x.ProductAttributeId);
            return productAttribute != null;
        }).WithMessage(
            translationService.GetResource("Api.Catalog.ProductAttributeMapping.Fields.ProductAttributeId.NotExists", languageId));
    }
}