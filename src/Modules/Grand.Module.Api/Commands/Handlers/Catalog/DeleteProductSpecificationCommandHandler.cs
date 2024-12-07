﻿using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteProductSpecificationCommandHandler : IRequestHandler<DeleteProductSpecificationCommand, bool>
{
    private readonly IProductService _productService;
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public DeleteProductSpecificationCommandHandler(ISpecificationAttributeService specificationAttributeService,
        IProductService productService)
    {
        _specificationAttributeService = specificationAttributeService;
        _productService = productService;
    }

    public async Task<bool> Handle(DeleteProductSpecificationCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Product.Id, true);
        var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == request.Id);
        if (psa != null) await _specificationAttributeService.DeleteProductSpecificationAttribute(psa, product.Id);

        return true;
    }
}