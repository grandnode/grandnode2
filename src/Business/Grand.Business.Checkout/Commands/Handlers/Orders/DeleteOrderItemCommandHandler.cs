using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
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
    public class DeleteOrderItemCommandHandler : IRequestHandler<DeleteOrderItemCommand, (bool error, string message)>
    {
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IGiftVoucherService _giftVoucherService;
        private readonly IInventoryManageService _inventoryManageService;

        public DeleteOrderItemCommandHandler(
            IMediator mediator,
            IOrderService orderService,
            IShipmentService shipmentService,
            IProductService productService,
            IGiftVoucherService giftVoucherService,
            IInventoryManageService inventoryManageService)
        {
            _mediator = mediator;
            _orderService = orderService;
            _shipmentService = shipmentService;
            _productService = productService;
            _giftVoucherService = giftVoucherService;
            _inventoryManageService = inventoryManageService;
        }

        public async Task<(bool error, string message)> Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            if (request.OrderItem == null)
                throw new ArgumentNullException(nameof(request.OrderItem));

            var product = await _productService.GetProductById(request.OrderItem.ProductId);
            if (product == null)
                return (true, "Product not exists.");

            if (request.OrderItem.OpenQty == 0 
                || request.OrderItem.Status == OrderItemStatus.Close 
                || request.OrderItem.OpenQty!= request.OrderItem.Quantity
                )
            {
                return (true, "You can't delete this order item.");
            }
            if (product.IsGiftVoucher)
            {
                return (true, "You can't cancel gift voucher, please delete it.");
            }

            var shipments = (await _shipmentService.GetShipmentsByOrder(request.Order.Id));
            foreach (var shipment in shipments)
            {
                if (shipment.ShipmentItems.Where(x => x.OrderItemId == request.OrderItem.Id).Any())
                {
                    return (true, $"This order item is in associated with shipment {shipment.ShipmentNumber}. Please delete it first.");
                }
            }
            if ((await _giftVoucherService.GetGiftVouchersByPurchasedWithOrderItemId(request.OrderItem.Id)).Count > 0)
            {
                //we cannot delete an order item with associated gift vouchers
                //a store owner should delete them first
                return (true, "This order item has an associated gift voucher record. Please delete it first.");
            }

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = $"Order item has been deleted - {product.Name}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = request.Order.Id,
            });

            await _inventoryManageService.AdjustReserved(product, request.OrderItem.Quantity, request.OrderItem.Attributes, request.OrderItem.WarehouseId);

            //delete item
            request.Order.OrderItems.Remove(request.OrderItem);

            await _orderService.DeleteOrderItem(request.OrderItem);

            request.Order.OrderSubtotalExclTax -= request.OrderItem.PriceExclTax;
            request.Order.OrderSubtotalInclTax -= request.OrderItem.PriceInclTax;
            request.Order.OrderTax -= (request.OrderItem.PriceInclTax - request.OrderItem.PriceExclTax);
            request.Order.OrderTotal -= request.OrderItem.PriceInclTax;
            
            if (request.Order.ShippingStatusId == ShippingStatus.PartiallyShipped)
            {
                if (!request.Order.HasItemsToAddToShipment() && !shipments.Where(x => x.DeliveryDateUtc == null).Any())
                {
                    request.Order.ShippingStatusId = ShippingStatus.Delivered;
                }
            }
            //TODO 
            //request.Order.OrderTaxes

            await _orderService.UpdateOrder(request.Order);

            //check order status
            await _mediator.Send(new CheckOrderStatusCommand() { Order = request.Order });

            return (false, "");
        }
    }
}
