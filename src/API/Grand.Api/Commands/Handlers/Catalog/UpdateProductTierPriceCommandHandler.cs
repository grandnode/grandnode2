using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateProductTierPriceCommandHandler : IRequestHandler<UpdateProductTierPriceCommand, bool>
    {
        private readonly IProductService _productService;

        public UpdateProductTierPriceCommandHandler(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<bool> Handle(UpdateProductTierPriceCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id, true);
            var tierPrice = request.Model.ToEntity();
            await _productService.UpdateTierPrice(tierPrice, product.Id);

            return true;
        }
    }
}
