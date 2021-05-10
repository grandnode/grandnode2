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
    public class UpdateOrderItemCommandHandler : IRequestHandler<UpdateOrderItemCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IInventoryManageService _inventoryManageService;

        public UpdateOrderItemCommandHandler(
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

        public async Task<bool> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            if (request.OrderItem == null)
                throw new ArgumentNullException(nameof(request.OrderItem));

            var originalOrder = await _orderService.GetOrderById(request.Order.Id);
            var originalOrderItem = originalOrder.OrderItems.FirstOrDefault(x => x.Id == request.OrderItem.Id);

            request.Order.OrderSubtotalExclTax += (request.OrderItem.PriceExclTax - originalOrderItem.PriceExclTax);
            request.Order.OrderSubtotalInclTax += (request.OrderItem.PriceInclTax - originalOrderItem.PriceInclTax);
            request.Order.OrderTax += 
                ((request.OrderItem.PriceInclTax - request.OrderItem.PriceExclTax)
                - (originalOrderItem.PriceInclTax - originalOrderItem.PriceExclTax));
            request.Order.OrderTotal += (request.OrderItem.PriceInclTax - originalOrderItem.PriceInclTax);

            //TODO 
            //request.Order.OrderTaxes

            //adjust inventory
            if (originalOrderItem.Quantity != request.OrderItem.Quantity)
            {
                int qtyDifference = originalOrderItem.Quantity - request.OrderItem.Quantity;
                var product = await _productService.GetProductById(request.OrderItem.ProductId);
                await _inventoryManageService.AdjustReserved(product, qtyDifference, request.OrderItem.Attributes, request.OrderItem.WarehouseId);

                if (request.Order.ShippingStatusId == ShippingStatus.PartiallyShipped)
                {
                    var shipments = (await _shipmentService.GetShipmentsByOrder(request.Order.Id));

                    if (!request.Order.HasItemsToAddToShipment() && !shipments.Where(x => x.DeliveryDateUtc == null).Any())
                    {
                        request.Order.ShippingStatusId = ShippingStatus.Delivered;
                    }
                }
            }
            await _orderService.UpdateOrder(request.Order);

            //check order status
            await _mediator.Send(new CheckOrderStatusCommand() { Order = request.Order });

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = "Order item has been edited",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = request.Order.Id,
            });

            return true;
        }
    }
}
