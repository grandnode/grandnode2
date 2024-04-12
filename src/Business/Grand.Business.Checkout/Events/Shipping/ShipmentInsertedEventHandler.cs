using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Business.Checkout.Events.Shipping;

/// <summary>
///     Update order items
/// </summary>
public class ShipmentInsertedEventHandler : INotificationHandler<EntityInserted<Shipment>>
{
    private readonly IInventoryManageService _inventoryManageService;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;

    public ShipmentInsertedEventHandler(IOrderService orderService, IProductService productService,
        IInventoryManageService inventoryManageService)
    {
        _orderService = orderService;
        _productService = productService;
        _inventoryManageService = inventoryManageService;
    }

    public async Task Handle(EntityInserted<Shipment> notification, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderById(notification.Entity.OrderId);
        if (order != null)
        {
            foreach (var item in notification.Entity.ShipmentItems)
            {
                var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
                if (orderItem == null) continue;
                orderItem.ShipQty += item.Quantity;
                orderItem.OpenQty -= item.Quantity;
                orderItem.Status = orderItem.OpenQty <= 0 ? OrderItemStatus.Close : OrderItemStatus.Open;
            }

            if (order.ShippingStatusId == ShippingStatus.Pending)
                order.ShippingStatusId = ShippingStatus.PreparedToShipped;

            await _orderService.UpdateOrder(order);
        }

        foreach (var item in notification.Entity.ShipmentItems)
        {
            var product = await _productService.GetProductById(item.ProductId);
            if (product != null)
                await _inventoryManageService.BookReservedInventory(product, notification.Entity, item);
        }
    }
}