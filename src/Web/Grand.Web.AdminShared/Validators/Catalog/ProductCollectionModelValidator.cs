using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class ProductCollectionModelValidator : BaseStoreAccessValidator<ProductModel.ProductCollectionModel, Product>
{
    private readonly IProductService _productService;

    public ProductCollectionModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.ProductCollectionModel>> validators,
        IRequestTranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
       : base(validators, translationService, contextAccessor)
    {
        _productService = productService;
    }
    protected override async Task<Product> GetEntity(ProductModel.ProductCollectionModel model)
    {
        return await _productService.GetProductById(model.ProductId);
    }
    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}
