using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class RelatedProductModelValidator : BaseStoreAccessValidator<ProductModel.RelatedProductModel, Product>
{
    private readonly IProductService _productService;

    public RelatedProductModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.RelatedProductModel>> validators,
        IRequestTranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
       : base(validators, translationService, contextAccessor)
    {
        _productService = productService;
    }
    protected override async Task<Product> GetEntity(ProductModel.RelatedProductModel model)
    {
        return await _productService.GetProductById(model.ProductId1);
    }
    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}
