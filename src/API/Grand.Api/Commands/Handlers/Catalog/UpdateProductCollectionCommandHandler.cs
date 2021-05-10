using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateProductCollectionCommandHandler : IRequestHandler<UpdateProductCollectionCommand, bool>
    {
        private readonly IProductCollectionService _productcollectionService;
        private readonly IProductService _productService;

        public UpdateProductCollectionCommandHandler(IProductCollectionService productcollectionService, IProductService productService)
        {
            _productcollectionService = productcollectionService;
            _productService = productService;
        }

        public async Task<bool> Handle(UpdateProductCollectionCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id, true);
            var productCollection = product.ProductCollections.Where(x => x.CollectionId == request.Model.CollectionId).FirstOrDefault();
            if (productCollection == null)
                throw new ArgumentException("No product collection mapping found with the specified id");

            productCollection.CollectionId = request.Model.CollectionId;
            productCollection.IsFeaturedProduct = request.Model.IsFeaturedProduct;

            await _productcollectionService.UpdateProductCollection(productCollection, product.Id);

            return true;
        }
    }
}
