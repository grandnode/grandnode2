using Grand.Business.Core.Extensions;
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

        var delivered = (await _shipmentService.GetShipmentsByOrder(request.Order.Id))
            .Where(x => x.DeliveryDateUtc.HasValue).ToList();

        //validate allowed number of days
        if (_orderSettings.NumberOfDaysMerchandiseReturnAvailable > 0)
        {
            var periodStart = _orderSettings.MerchandiseReturnPeriodStart == MerchandiseReturnPeriodStart.Delivered
                ? delivered.Max(x => x.DeliveryDateUtc)
                : request.Order.CreatedOnUtc;
            if (periodStart.HasValue && (DateTime.UtcNow - periodStart.Value).TotalDays >=
                _orderSettings.NumberOfDaysMerchandiseReturnAvailable)
                return quantities;
        }

        var merchandiseReturns =
            await _mediator.Send(new GetMerchandiseReturnQuery { OrderId = request.Order.Id }, cancellationToken);

        foreach (var item in request.Order.OrderItems)
        {
            var product = await _productService.GetProductByIdIncludeArch(item.ProductId);
            if (product == null || product.NotReturnable)
                continue;

            var qtyDelivery = delivered.SelectMany(x => x.ShipmentItems)
                .Where(x => x.OrderItemId == item.Id).Sum(x => x.Quantity);
            quantities[item.Id] = Math.Max(0, qtyDelivery - merchandiseReturns.ReturnedQuantity(item.Id));
        }

        return quantities;
    }
}
