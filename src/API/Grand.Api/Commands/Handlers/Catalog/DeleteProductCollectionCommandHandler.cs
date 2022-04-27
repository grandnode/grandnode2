﻿using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductCollectionCommandHandler : IRequestHandler<DeleteProductCollectionCommand, bool>
    {
        private readonly IProductCollectionService _productcollectionService;
        private readonly IProductService _productService;

        public DeleteProductCollectionCommandHandler(IProductCollectionService productcollectionService, IProductService productService)
        {
            _productcollectionService = productcollectionService;
            _productService = productService;
        }

        public async Task<bool> Handle(DeleteProductCollectionCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id, true);
            var productCollection = product.ProductCollections.Where(x => x.CollectionId == request.CollectionId).FirstOrDefault();
            if (productCollection == null)
                throw new ArgumentException("No product collection mapping found with the specified id");

            await _productcollectionService.DeleteProductCollection(productCollection, product.Id);

            return true;
        }
    }
}
