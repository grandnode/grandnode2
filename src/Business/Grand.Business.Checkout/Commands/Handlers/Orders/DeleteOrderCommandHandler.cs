using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IInventoryManageService _inventoryManageService;
        private readonly IProductReservationService _productReservationService;
        private readonly IAuctionService _auctionService;
        private readonly IDiscountService _discountService;
        private readonly OrderSettings _orderSettings;

        public DeleteOrderCommandHandler(
            IMediator mediator,
            IOrderService orderService,
            IShipmentService shipmentService,
            IProductService productService,
            IInventoryManageService inventoryManageService,
            IProductReservationService productReservationService,
            IAuctionService auctionService,
            IDiscountService discountService,
            OrderSettings orderSettings)
        {
            _mediator = mediator;
            _orderService = orderService;
            _shipmentService = shipmentService;
            _productService = productService;
            _inventoryManageService = inventoryManageService;
            _productReservationService = productReservationService;
            _auctionService = auctionService;
            _discountService = discountService;
            _orderSettings = orderSettings;
        }

        public async Task<bool> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            var shipments = await _shipmentService.GetShipmentsByOrder(request.Order.Id);
            if (shipments.Any())
                throw new Exception("Cannot do delete for order with shipments");

            //check whether the order wasn't cancelled before
            if (request.Order.OrderStatusId != (int)OrderStatusSystem.Cancelled)
            {
                //return (add) back redeemded loyalty points
                await _mediator.Send(new ReturnBackRedeemedLoyaltyPointsCommand() { Order = request.Order });

                //reduce (cancel) back loyalty points (previously awarded for this order)
                await _mediator.Send(new ReduceLoyaltyPointsCommand() { Order = request.Order });

                //Adjust inventory
                foreach (var orderItem in request.Order.OrderItems)
                {
                    var product = await _productService.GetProductById(orderItem.ProductId);
                    if (product != null)
                        await _inventoryManageService.AdjustReserved(product, orderItem.Quantity-orderItem.CancelQty, orderItem.Attributes, orderItem.WarehouseId);
                }

                //cancel reservations
                await _productReservationService.CancelReservationsByOrderId(request.Order.Id);

                //cancel bid
                await _auctionService.CancelBidByOrder(request.Order.Id);
            }

            //deactivate gift vouchers
            if (_orderSettings.DeactivateGiftVouchersAfterDeletingOrder)
                await _mediator.Send(new ActivatedValueForPurchasedGiftVouchersCommand() { Order = request.Order, Activate = false });

            request.Order.Deleted = true;
            //now delete an order
            await _orderService.UpdateOrder(request.Order);

            //cancel discounts 
            await _discountService.CancelDiscount(request.Order.Id);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = $"Order has been deleted",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = request.Order.Id,
            });

            //event notification
            await _mediator.Publish(new OrderDeletedEvent(request.Order));

            return true;
        }
    }
}
