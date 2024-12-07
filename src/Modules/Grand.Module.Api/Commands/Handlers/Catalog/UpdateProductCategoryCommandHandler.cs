﻿using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateProductCategoryCommandHandler : IRequestHandler<UpdateProductCategoryCommand, bool>
{
    private readonly IProductCategoryService _productCategoryService;
    private readonly IProductService _productService;

    public UpdateProductCategoryCommandHandler(
        IProductCategoryService productCategoryService,
        IProductService productService)
    {
        _productCategoryService = productCategoryService;
        _productService = productService;
    }

    public async Task<bool> Handle(UpdateProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Product.Id, true);
        var productCategory = product.ProductCategories.FirstOrDefault(x => x.CategoryId == request.Model.CategoryId);
        if (productCategory == null)
            throw new ArgumentException("No product category mapping found with the specified id");

        productCategory.CategoryId = request.Model.CategoryId;
        productCategory.IsFeaturedProduct = request.Model.IsFeaturedProduct;

        await _productCategoryService.UpdateProductCategory(productCategory, product.Id);

        return true;
    }
}