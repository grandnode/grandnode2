using FluentValidation;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class BundleProductModelValidator : BaseStoreAccessValidator<ProductModel.BundleProductModel, Product>
{
    private readonly IProductService _productService;
    public BundleProductModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.BundleProductModel>> validators,
        ITranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
        : base(validators, translationService, contextAccessor)
    {
        _productService = productService;
    }
    protected override async Task<Product> GetEntity(ProductModel.BundleProductModel model)
    {
        return await _productService.GetProductById(model.ProductId);
    }

    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}