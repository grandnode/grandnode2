﻿using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddProductPictureCommandHandler : IRequestHandler<AddProductPictureCommand, bool>
{
    private readonly IPictureService _pictureService;
    private readonly IProductService _productService;

    public AddProductPictureCommandHandler(
        IProductService productService,
        IPictureService pictureService)
    {
        _productService = productService;
        _pictureService = pictureService;
    }

    public async Task<bool> Handle(AddProductPictureCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Product.Id);
        if (product == null)
            return false;

        var picture = await _pictureService.GetPictureById(request.Model.PictureId);
        if (picture == null)
            return false;

        await _productService.InsertProductPicture(new ProductPicture {
            PictureId = picture.Id,
            DisplayOrder = request.Model.DisplayOrder,
            IsDefault = request.Model.IsDefault
        }, product.Id);

        return true;
    }
}