using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class GetReturnableQuantitiesQueryHandler
    : IRequestHandler<GetReturnableQuantitiesQuery, IReadOnlyDictionary<string, int>>
{
    private readonly IMediator _mediator;
    private readonly OrderSettings _orderSettings;
    private readonly IProductService _productService;
    private readonly IShipmentService _shipmentService;

    public GetReturnableQuantitiesQueryHandler(
        IShipmentService shipmentService,
        IProductService productService,
        IMediator mediator,
        OrderSettings orderSettings)
    {
        _shipmentService = shipmentService;
        _productService = productService;
        _mediator = mediator;
        _orderSettings = orderSettings;
    }

    public async Task<IReadOnlyDictionary<string, int>> Handle(GetReturnableQuantitiesQuery request,
        CancellationToken cancellationToken)
    {
        var quantities = request.Order.OrderItems.ToDictionary(x => x.Id, _ => 0);

        //validate allowed number of days
        var days = request.ForStaff ? 0 : _orderSettings.NumberOfDaysMerchandiseReturnAvailable;
        var fromDelivery = _orderSettings.MerchandiseReturnPeriodStart == MerchandiseReturnPeriodStart.Delivered;
        if (days > 0 && !fromDelivery && (DateTime.UtcNow - request.Order.CreatedOnUtc).TotalDays >= days)
            return quantities;

        var delivered = (await _shipmentService.GetShipmentsByOrder(request.Order.Id))
            .Where(x => x.DeliveryDateUtc.HasValue).ToList();
        //nothing delivered, nothing to return: spares the queries below for every undelivered order of a list
        if (delivered.Count == 0)
            return quantities;

        var merchandiseReturns =
            await _mediator.Send(new GetMerchandiseReturnQuery { OrderId = request.Order.Id }, cancellationToken);

        var computed = ReturnableQuantities.Compute(request.Order, delivered, merchandiseReturns, days, fromDelivery,
            DateTime.UtcNow);
        foreach (var item in request.Order.OrderItems)
        {
            //the product is read only for an item that has something left
            if (computed[item.Id] <= 0)
                continue;

            if (!request.ForStaff)
            {
                var product = await _productService.GetProductByIdIncludeArch(item.ProductId);
                if (product == null || product.NotReturnable)
                    continue;
            }

            quantities[item.Id] = computed[item.Id];
        }

        return quantities;
    }
}
