using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

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