using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Validators.Catalog;

public class AddProductSpecificationAttributeModelValidator : BaseGrandValidator<
    ProductModel.AddProductSpecificationAttributeModel>
{
    public AddProductSpecificationAttributeModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.AddProductSpecificationAttributeModel>> validators,
        ITranslationService translationService,
        ISpecificationAttributeService specificationAttributeService)
        : base(validators)
    {
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (x.AttributeTypeId == SpecificationAttributeType.Option)
            {
                if (string.IsNullOrEmpty(x.SpecificationAttributeId))
                    return false;
                if (string.IsNullOrEmpty(x.SpecificationAttributeOptionId))
                    return false;

                var specification =
                    await specificationAttributeService.GetSpecificationAttributeById(x.SpecificationAttributeId);

                return specification?.SpecificationAttributeOptions.FirstOrDefault(z =>
                    z.Id == x.SpecificationAttributeOptionId) != null;
            }

            return !string.IsNullOrEmpty(x.CustomValue);
        }).WithMessage(translationService.GetResource("Vendor.Catalog.Products.SpecificationAttributes.Validate"));
    }
}