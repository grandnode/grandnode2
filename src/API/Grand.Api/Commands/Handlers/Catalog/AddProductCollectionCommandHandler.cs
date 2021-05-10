using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Domain.Catalog;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddProductCollectionCommandHandler : IRequestHandler<AddProductCollectionCommand, bool>
    {
        private readonly IProductCollectionService _productcollectionService;

        public AddProductCollectionCommandHandler(IProductCollectionService productcollectionService)
        {
            _productcollectionService = productcollectionService;
        }

        public async Task<bool> Handle(AddProductCollectionCommand request, CancellationToken cancellationToken)
        {
            var productCollection = new ProductCollection
            {
                CollectionId = request.Model.CollectionId,
                IsFeaturedProduct = request.Model.IsFeaturedProduct,
            };
            await _productcollectionService.InsertProductCollection(productCollection, request.Product.Id);

            return true;
        }
    }
}
