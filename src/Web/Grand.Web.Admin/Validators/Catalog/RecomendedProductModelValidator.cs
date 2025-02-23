﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class RecomendedProductModelValidator : BaseStoreAccessValidator<ProductModel.RecommendedProductModel, Product>
{
    private readonly IProductService _productService;

    public RecomendedProductModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.RecommendedProductModel>> validators,
        ITranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
       : base(validators, translationService, contextAccessor)
    {
        _productService = productService;
    }
    protected override async Task<Product> GetEntity(ProductModel.RecommendedProductModel model)
    {
        return await _productService.GetProductById(model.ProductId);
    }
    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}
