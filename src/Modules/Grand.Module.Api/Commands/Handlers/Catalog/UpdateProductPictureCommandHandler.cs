﻿using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateProductPictureCommandHandler : IRequestHandler<UpdateProductPictureCommand, bool>
{
    private readonly IProductService _productService;

    public UpdateProductPictureCommandHandler(
        IProductService productService)
    {
        _productService = productService;
    }

    public async Task<bool> Handle(UpdateProductPictureCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Product.Id);
        var pp = product.ProductPictures.FirstOrDefault(x => x.PictureId == request.Model.PictureId);
        if (pp != null)
        {
            pp.DisplayOrder = request.Model.DisplayOrder;
            pp.IsDefault = request.Model.IsDefault;
            await _productService.UpdateProductPicture(pp, request.Product.Id);
        }

        return true;
    }
}