﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class IsMerchandiseReturnAllowedQueryHandler : IRequestHandler<IsMerchandiseReturnAllowedQuery, bool>
    {
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IMediator _mediator;
        private readonly OrderSettings _orderSettings;

        public IsMerchandiseReturnAllowedQueryHandler(
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

        public async Task<bool> Handle(IsMerchandiseReturnAllowedQuery request, CancellationToken cancellationToken)
        {
            if (!_orderSettings.MerchandiseReturnsEnabled)
                return false;

            if (request.Order == null || request.Order.Deleted)
                return false;

            var shipments = await _shipmentService.GetShipmentsByOrder(request.Order.Id);

            //validate allowed number of days
            if (_orderSettings.NumberOfDaysMerchandiseReturnAvailable > 0)
            {
                var daysPassed = (DateTime.UtcNow - request.Order.CreatedOnUtc).TotalDays;
                if (daysPassed >= _orderSettings.NumberOfDaysMerchandiseReturnAvailable)
                    return false;
            }
            foreach (var item in request.Order.OrderItems)
            {
                var product = await _productService.GetProductById(item.ProductId);
                if (product == null)
                    return false;

                var qtyDelivery = shipments.Where(x => x.DeliveryDateUtc.HasValue).SelectMany(x => x.ShipmentItems).Where(x => x.OrderItemId == item.Id).Sum(x => x.Quantity);
                var merchandiseReturns = (await _mediator.Send(new GetMerchandiseReturnQuery { OrderItemId = item.Id }, cancellationToken)).ToList();
                var qtyReturn = merchandiseReturns.Sum(rr => rr.MerchandiseReturnItems.Sum(rrItem => rrItem.Quantity));

                if (!product.NotReturnable && qtyDelivery - qtyReturn > 0)
                    return true;
            }
            return false;

        }
    }
}
