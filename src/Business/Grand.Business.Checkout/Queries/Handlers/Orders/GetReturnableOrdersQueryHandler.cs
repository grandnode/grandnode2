using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

/// <summary>
///     The rules of GetReturnableQuantitiesQuery for the customer, for many orders in three queries: their delivered
///     shipments, their returns and the products of the items with something left
/// </summary>
public class GetReturnableOrdersQueryHandler : IRequestHandler<GetReturnableOrdersQuery, IReadOnlySet<string>>
{
    private readonly IRepository<MerchandiseReturn> _merchandiseReturnRepository;
    private readonly OrderSettings _orderSettings;
    private readonly IRepository<Product> _productRepository;
    private readonly IProductService _productService;
    private readonly IRepository<Shipment> _shipmentRepository;

    public GetReturnableOrdersQueryHandler(
        IRepository<Shipment> shipmentRepository,
        IRepository<MerchandiseReturn> merchandiseReturnRepository,
        IRepository<Product> productRepository,
        IProductService productService,
        OrderSettings orderSettings)
    {
        _shipmentRepository = shipmentRepository;
        _merchandiseReturnRepository = merchandiseReturnRepository;
        _productRepository = productRepository;
        _productService = productService;
        _orderSettings = orderSettings;
    }

    public async Task<IReadOnlySet<string>> Handle(GetReturnableOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var returnable = new HashSet<string>();
        if (!_orderSettings.MerchandiseReturnsEnabled)
            return returnable;

        var utcNow = DateTime.UtcNow;
        var days = _orderSettings.NumberOfDaysMerchandiseReturnAvailable;
        var fromDelivery = _orderSettings.MerchandiseReturnPeriodStart == MerchandiseReturnPeriodStart.Delivered;
        var orders = request.Orders.Where(x => !x.Deleted &&
                                               !(days > 0 && !fromDelivery &&
                                                 (utcNow - x.CreatedOnUtc).TotalDays >= days)).ToList();
        if (orders.Count == 0)
            return returnable;

        var orderIds = orders.Select(x => x.Id).ToList();
        var delivered = await _shipmentRepository.ToListAsync(_shipmentRepository.Table
            .Where(x => orderIds.Contains(x.OrderId) && x.DeliveryDateUtc != null), cancellationToken);
        //nothing delivered, nothing to return: an order list of recent orders stops here
        if (delivered.Count == 0)
            return returnable;

        var merchandiseReturns = await _merchandiseReturnRepository.ToListAsync(_merchandiseReturnRepository.Table
            .Where(x => orderIds.Contains(x.OrderId)), cancellationToken);

        var left = orders.ToDictionary(x => x.Id, order => ReturnableQuantities.Compute(order,
            delivered.Where(x => x.OrderId == order.Id).ToList(),
            merchandiseReturns.Where(x => x.OrderId == order.Id).ToList(), days, fromDelivery, utcNow));

        var productIds = orders.SelectMany(order => order.OrderItems.Where(item => left[order.Id][item.Id] > 0))
            .Select(x => x.ProductId).Distinct().ToList();
        if (productIds.Count == 0)
            return returnable;

        var notReturnable = (await _productRepository.ToListAsync(_productRepository.Table
                .Where(x => productIds.Contains(x.Id)).Select(x => new { x.Id, x.NotReturnable }), cancellationToken))
            .ToDictionary(x => x.Id, x => x.NotReturnable);
        //a product not in the catalog any more may be in its archive; one that is in neither cannot be returned
        foreach (var productId in productIds.Where(x => !notReturnable.ContainsKey(x)))
            notReturnable[productId] = (await _productService.GetProductByIdIncludeArch(productId))?.NotReturnable ?? true;

        foreach (var order in orders)
            if (order.OrderItems.Any(item => left[order.Id][item.Id] > 0 && !notReturnable[item.ProductId]))
                returnable.Add(order.Id);

        return returnable;
    }
}
