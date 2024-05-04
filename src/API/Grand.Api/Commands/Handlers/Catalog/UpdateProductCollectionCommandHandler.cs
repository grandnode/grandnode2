using Grand.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class UpdateProductCollectionCommandHandler : IRequestHandler<UpdateProductCollectionCommand, bool>
{
    private readonly IProductCollectionService _productCollectionService;
    private readonly IProductService _productService;

    public UpdateProductCollectionCommandHandler(IProductCollectionService productCollectionService,
        IProductService productService)
    {
        _productCollectionService = productCollectionService;
        _productService = productService;
    }

    public async Task<bool> Handle(UpdateProductCollectionCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Product.Id, true);
        var productCollection =
            product.ProductCollections.FirstOrDefault(x => x.CollectionId == request.Model.CollectionId);
        if (productCollection == null)
            throw new ArgumentException("No product collection mapping found with the specified id");

        productCollection.CollectionId = request.Model.CollectionId;
        productCollection.IsFeaturedProduct = request.Model.IsFeaturedProduct;

        await _productCollectionService.UpdateProductCollection(productCollection, product.Id);

        return true;
    }
}