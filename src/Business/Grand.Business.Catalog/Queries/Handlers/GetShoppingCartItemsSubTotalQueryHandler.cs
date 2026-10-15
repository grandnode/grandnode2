using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Mediator;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetShoppingCartItemsSubTotalQueryHandler : IRequestHandler<GetShoppingCartItemsSubTotalQuery, double>
{
    private readonly IPricingService _pricingService;
    private readonly IProductService _productService;

    public GetShoppingCartItemsSubTotalQueryHandler(IPricingService pricingService, IProductService productService)
    {
        _pricingService = pricingService;
        _productService = productService;
    }

    public async Task<double> Handle(GetShoppingCartItemsSubTotalQuery request, CancellationToken cancellationToken)
    {
        double subTotal = 0;
        foreach (var item in request.Items)
        {
            var product = await _productService.GetProductById(item.ProductId);
            if (product != null)
                subTotal += (await _pricingService.GetSubTotal(item, product, false)).subTotal;
        }

        return subTotal;
    }
}
