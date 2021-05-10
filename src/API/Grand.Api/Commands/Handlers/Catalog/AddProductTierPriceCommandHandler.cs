using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
