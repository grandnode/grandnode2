using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Catalog;

namespace Grand.Web.Vendor.Validators.Catalog;

public class ProductAttributeValueModelValidator : BaseGrandValidator<ProductModel.ProductAttributeValueModel>
{
    public ProductAttributeValueModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.ProductAttributeValueModel>> validators,
        ITranslationService translationService, IProductService productService)
        : base(validators)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(translationService.GetResource(
                "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Name.Required"));

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .WithMessage(translationService.GetResource(
                "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Quantity.GreaterThanOrEqualTo1"))
            .When(x => x.AttributeValueTypeId == AttributeValueType.AssociatedToProduct);

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var product = await productService.GetProductById(x.ProductId);
            var productAttributeMapping =
                product.ProductAttributeMappings.FirstOrDefault(y => y.Id == x.ProductAttributeMappingId);
            switch (productAttributeMapping?.AttributeControlTypeId)
            {
                case AttributeControlType.ColorSquares:
                {
                    //ensure valid color is chosen/entered
                    if (string.IsNullOrEmpty(x.ColorSquaresRgb))
                        context.AddFailure("Color is required");
                    break;
                }
                //ensure a picture is uploaded
                case AttributeControlType.ImageSquares when string.IsNullOrEmpty(x.ImageSquaresPictureId):
                    context.AddFailure("Image is required");
                    break;
            }
        });
    }
}