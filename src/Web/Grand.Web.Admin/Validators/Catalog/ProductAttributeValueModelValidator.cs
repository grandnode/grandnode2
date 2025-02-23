using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class ProductAttributeValueModelValidator : BaseStoreAccessValidator<ProductModel.ProductAttributeValueModel, Product>
{
    private readonly IProductService _productService;
    public ProductAttributeValueModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.ProductAttributeValueModel>> validators,
        ITranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
        : base(validators, translationService, contextAccessor)
    {
        _productService = productService;
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Name.Required"));

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .WithMessage(translationService.GetResource(
                "Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Quantity.GreaterThanOrEqualTo1"))
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
    protected override async Task<Product> GetEntity(ProductModel.ProductAttributeValueModel model)
    {
        return await _productService.GetProductById(model.ProductId);
    }

    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}