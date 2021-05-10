using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class CancelOrderItemCommandHandler : IRequestHandler<CancelOrderItemCommand, (bool error, string message)>
    {
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IInventoryManageService _inventoryManageService;

        public CancelOrderItemCommandHandler(
            IMediator mediator,
            IOrderService orderService,
            IShipmentService shipmentService,
            IProductService productService,
            IInventoryManageService inventoryManageService)
        {
            _mediator = mediator;
            _orderService = orderService;
            _shipmentService = shipmentService;
            _productService = productService;
            _inventoryManageService = inventoryManageService;
        }

        public async Task<(bool error, string message)> Handle(CancelOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            if (request.OrderItem == null)
                throw new ArgumentNullException(nameof(request.OrderItem));

            var product = await _productService.GetProductById(request.OrderItem.ProductId);
            if (product == null)
                return (true, "Product not exists.");

            if (request.OrderItem.OpenQty == 0 || request.OrderItem.Status == OrderItemStatus.Close)
            {
                return (true, "You can't cancel this order item.");
            }
            if (product.IsGiftVoucher)
            {
                return (true, "You can't cancel gift voucher, please delete it.");
            }

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = $"Order item has been canceled - {product.Name} - Qty: {request.OrderItem.OpenQty}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = request.Order.Id,
            });

            await _inventoryManageService.AdjustReserved(product, request.OrderItem.OpenQty, request.OrderItem.Attributes, request.OrderItem.WarehouseId);

            request.OrderItem.CancelQty = request.OrderItem.OpenQty;
            request.OrderItem.CancelAmount = Math.Round(request.OrderItem.UnitPriceInclTax / request.OrderItem.CancelQty, 2);
            request.OrderItem.OpenQty = 0;
            request.OrderItem.Status = OrderItemStatus.Close;

            if (request.Order.ShippingStatusId == ShippingStatus.PartiallyShipped)
            {
                var shipments = await _shipmentService.GetShipmentsByOrder(request.Order.Id);
                if (!request.Order.HasItemsToAddToShipment() && !shipments.Where(x => x.ShippedDateUtc == null).Any())
                {
                    request.Order.ShippingStatusId = ShippingStatus.Shipped;
                }
                if (!request.Order.HasItemsToAddToShipment() && !shipments.Where(x => x.DeliveryDateUtc == null).Any())
                {
                    request.Order.ShippingStatusId = ShippingStatus.Delivered;
                }
            }

            await _orderService.UpdateOrder(request.Order);

            //check order status
            await _mediator.Send(new CheckOrderStatusCommand() { Order = request.Order });

            return (false, "");
        }
    }
}
