using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
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

        var delivered = (await _shipmentService.GetShipmentsByOrder(request.Order.Id))
            .Where(x => x.DeliveryDateUtc.HasValue).ToList();

        //validate allowed number of days
        var days = _orderSettings.NumberOfDaysMerchandiseReturnAvailable;
        var withinPeriod = delivered;
        if (days > 0)
        {
            if (_orderSettings.MerchandiseReturnPeriodStart == MerchandiseReturnPeriodStart.Delivered)
                //each shipment has its own days, counted from its delivery
                withinPeriod = delivered.Where(x => (DateTime.UtcNow - x.DeliveryDateUtc!.Value).TotalDays < days)
                    .ToList();
            else if ((DateTime.UtcNow - request.Order.CreatedOnUtc).TotalDays >= days)
                return quantities;
        }

        var merchandiseReturns =
            await _mediator.Send(new GetMerchandiseReturnQuery { OrderId = request.Order.Id }, cancellationToken);

        foreach (var item in request.Order.OrderItems)
        {
            var product = await _productService.GetProductByIdIncludeArch(item.ProductId);
            if (product == null || product.NotReturnable)
                continue;

            //returns do not say which shipment they came from, so the quantity still within its period caps what is
            //left of everything delivered
            var qtyDelivery = DeliveredQuantity(delivered, item.Id);
            var qtyWithinPeriod = DeliveredQuantity(withinPeriod, item.Id);
            quantities[item.Id] = Math.Max(0,
                Math.Min(qtyWithinPeriod, qtyDelivery - merchandiseReturns.ReturnedQuantity(item.Id)));
        }

        return quantities;
    }

    private static int DeliveredQuantity(IEnumerable<Shipment> shipments, string orderItemId)
    {
        return shipments.SelectMany(x => x.ShipmentItems).Where(x => x.OrderItemId == orderItemId)
            .Sum(x => x.Quantity);
    }
}
