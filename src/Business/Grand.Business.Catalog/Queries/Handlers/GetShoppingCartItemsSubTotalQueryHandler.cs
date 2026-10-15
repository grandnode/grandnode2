using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Catalog;
using Grand.Mediator;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetShoppingCartItemsSubTotalQueryHandler : IRequestHandler<GetShoppingCartItemsSubTotalQuery, double>
{
    private readonly ICurrencyService _currencyService;
    private readonly IPricingService _pricingService;
    private readonly IProductService _productService;

    public GetShoppingCartItemsSubTotalQueryHandler(
        IPricingService pricingService,
        IProductService productService,
        ICurrencyService currencyService)
    {
        _pricingService = pricingService;
        _productService = productService;
        _currencyService = currencyService;
    }

    public async Task<double> Handle(GetShoppingCartItemsSubTotalQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Store);

        //priced for the customer of the request, not the ambient one, in the currency the requirement is entered in
        var currency = await _currencyService.GetPrimaryStoreCurrency();

        double subTotal = 0;
        foreach (var item in request.Items)
        {
            var product = await _productService.GetProductById(item.ProductId);
            if (product == null)
                continue;

            //without discounts the subtotal is the unit price times the quantity, as in IPricingService.GetSubTotal
            var unitPrice = await _pricingService.GetUnitPrice(product, request.Customer, request.Store, currency,
                item.ShoppingCartTypeId, item.Quantity, item.Attributes, item.EnteredPrice,
                item.RentalStartDateUtc, item.RentalEndDateUtc, false);
            subTotal += unitPrice.unitprice * item.Quantity;
        }

        return subTotal;
    }
}
