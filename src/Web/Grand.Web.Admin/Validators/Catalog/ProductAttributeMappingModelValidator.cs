﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class ProductAttributeMappingModelValidator : BaseStoreAccessValidator<ProductModel.ProductAttributeMappingModel, Product>
{
    private readonly IProductService _productService;

    public ProductAttributeMappingModelValidator(
        IEnumerable<IValidatorConsumer<ProductModel.ProductAttributeMappingModel>> validators,
        ITranslationService translationService, IProductService productService, IContextAccessor contextAccessor)
       : base(validators, translationService, contextAccessor)
    {
        _productService = productService;
    }
    protected override async Task<Product> GetEntity(ProductModel.ProductAttributeMappingModel model)
    {
        return await _productService.GetProductById(model.ProductId);
    }

    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}