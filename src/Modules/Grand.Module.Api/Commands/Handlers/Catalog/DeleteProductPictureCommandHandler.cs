﻿using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteProductPictureCommandHandler : IRequestHandler<DeleteProductPictureCommand, bool>
{
    private readonly IProductService _productService;

    public DeleteProductPictureCommandHandler(
        IProductService productService)
    {
        _productService = productService;
    }

    public async Task<bool> Handle(DeleteProductPictureCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Product.Id);

        var productPicture = product.ProductPictures.FirstOrDefault(x => x.PictureId == request.PictureId);
        if (productPicture == null)
            throw new ArgumentException("No product picture found with the specified pictureid");

        await _productService.DeleteProductPicture(productPicture, product.Id);

        return true;
    }
}