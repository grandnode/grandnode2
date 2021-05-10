using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Events.Shipping
{
    /// <summary>
    /// Update order items 
    /// </summary>
    public class ShipmentDeletedEventHandler : INotificationHandler<EntityDeleted<Shipment>>
    {
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IInventoryManageService _inventoryManageService;
        public ShipmentDeletedEventHandler(
            IOrderService orderService, 
            IShipmentService shipmentService,
            IProductService productService,
            IInventoryManageService inventoryManageService)
        {
            _orderService = orderService;
            _shipmentService = shipmentService;
            _productService = productService;
            _inventoryManageService = inventoryManageService;
        }

        public async Task Handle(EntityDeleted<Shipment> notification, CancellationToken cancellationToken)
        {
            //reverse booked invetory
            foreach (var shipmentItem in notification.Entity.ShipmentItems)
            {
                await _inventoryManageService.ReverseBookedInventory(notification.Entity, shipmentItem);
            }


            var order = await _orderService.GetOrderById(notification.Entity.OrderId);
            if (order != null)
            {
                foreach (var item in notification.Entity.ShipmentItems)
                {
                    var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
                    if (orderItem != null)
                    {
                        orderItem.ShipQty -= item.Quantity;
                        orderItem.OpenQty += item.Quantity;
                        orderItem.Status = orderItem.OpenQty <= 0 ? Domain.Orders.OrderItemStatus.Close : Domain.Orders.OrderItemStatus.Open;
                    }
                }

                if (!order.OrderItems.Where(x => x.ShipQty > 0).Any())
                {
                    order.ShippingStatusId = ShippingStatus.Pending;
                }
                else
                {
                    if (order.ShippingStatusId == ShippingStatus.Delivered)
                        order.ShippingStatusId = ShippingStatus.PartiallyShipped;
                    else
                    {
                        var shipments = await _shipmentService.GetShipmentsByOrder(order.Id);
                        if (shipments.Any())
                        {
                            if (shipments.Where(x => x.ShippedDateUtc == null).Any())
                                order.ShippingStatusId = ShippingStatus.PreparedToShipped;
                            if (shipments.Where(x => x.ShippedDateUtc != null).Any())
                                order.ShippingStatusId = ShippingStatus.PartiallyShipped;
                        }
                    }
                }
                await _orderService.UpdateOrder(order);
            }
        }
    }
}
