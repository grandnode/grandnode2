using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductTierPriceCommandHandler : IRequestHandler<DeleteProductTierPriceCommand, bool>
    {
        private readonly IProductService _productService;

        public DeleteProductTierPriceCommandHandler(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<bool> Handle(DeleteProductTierPriceCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id, true);
            var tierPrice = product.TierPrices.Where(x => x.Id == request.Id).FirstOrDefault();

            await _productService.DeleteTierPrice(tierPrice, product.Id);

            return true;
        }
    }
}
