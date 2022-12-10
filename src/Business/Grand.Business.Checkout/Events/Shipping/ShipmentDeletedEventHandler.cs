using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Business.Checkout.Events.Shipping
{
    /// <summary>
    /// Update order items 
    /// </summary>
    public class ShipmentDeletedEventHandler : INotificationHandler<EntityDeleted<Shipment>>
    {
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IInventoryManageService _inventoryManageService;
        public ShipmentDeletedEventHandler(
            IOrderService orderService, 
            IShipmentService shipmentService,
            IInventoryManageService inventoryManageService)
        {
            _orderService = orderService;
            _shipmentService = shipmentService;
            _inventoryManageService = inventoryManageService;
        }

        public async Task Handle(EntityDeleted<Shipment> notification, CancellationToken cancellationToken)
        {
            //reverse booked inventory
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
                    if (orderItem == null) continue;
                    orderItem.ShipQty -= item.Quantity;
                    orderItem.OpenQty += item.Quantity;
                    orderItem.Status = orderItem.OpenQty <= 0 ? Domain.Orders.OrderItemStatus.Close : Domain.Orders.OrderItemStatus.Open;
                }

                if (!order.OrderItems.Any(x => x.ShipQty > 0))
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
                            if (shipments.Any(x => x.ShippedDateUtc == null))
                                order.ShippingStatusId = ShippingStatus.PreparedToShipped;
                            if (shipments.Any(x => x.ShippedDateUtc != null))
                                order.ShippingStatusId = ShippingStatus.PartiallyShipped;
                        }
                    }
                }
                await _orderService.UpdateOrder(order);
            }
        }
    }
}
