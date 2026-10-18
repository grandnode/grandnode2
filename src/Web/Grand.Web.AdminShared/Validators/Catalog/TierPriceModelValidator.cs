using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class TierPriceModelValidator : BaseStoreAccessValidator<ProductModel.TierPriceModel, Product>
{
    private readonly IProductService _productService;

    public TierPriceModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.TierPriceModel>> validators,
        IRequestTranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
       : base(validators, translationService, contextAccessor)
    {
        _productService = productService;
    }
    protected override async Task<Product> GetEntity(ProductModel.TierPriceModel model)
    {
        return await _productService.GetProductById(model.ProductId);
    }
    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}

public class TierPriceDeleteModelValidator : BaseStoreAccessValidator<ProductModel.TierPriceDeleteModel, Product>
{
    private readonly IProductService _productService;

    public TierPriceDeleteModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.TierPriceDeleteModel>> validators,
        IRequestTranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
       : base(validators, translationService, contextAccessor)
    {
        _productService = productService;
    }
    protected override async Task<Product> GetEntity(ProductModel.TierPriceDeleteModel model)
    {
        return await _productService.GetProductById(model.ProductId);
    }
    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}