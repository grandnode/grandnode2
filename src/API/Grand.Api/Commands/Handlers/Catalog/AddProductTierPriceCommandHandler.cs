using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddProductTierPriceCommandHandler : IRequestHandler<AddProductTierPriceCommand, bool>
    {
        private readonly IProductService _productService;

        public AddProductTierPriceCommandHandler(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<bool> Handle(AddProductTierPriceCommand request, CancellationToken cancellationToken)
        {
            var tierPrice = request.Model.ToEntity();
            await _productService.InsertTierPrice(tierPrice, request.Product.Id);

            return true;
        }
    }
}
