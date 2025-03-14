﻿using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddProductCategoryCommandHandler : IRequestHandler<AddProductCategoryCommand, bool>
{
    private readonly IProductCategoryService _productcategoryService;

    public AddProductCategoryCommandHandler(IProductCategoryService productcategoryService)
    {
        _productcategoryService = productcategoryService;
    }

    public async Task<bool> Handle(AddProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var productCategory = new ProductCategory {
            CategoryId = request.Model.CategoryId,
            IsFeaturedProduct = request.Model.IsFeaturedProduct
        };
        await _productcategoryService.InsertProductCategory(productCategory, request.Product.Id);

        return true;
    }
}