using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Events.Orders;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Checkout.Interfaces.Payments;
using System.Linq;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IProductService _productService;
        private readonly IInventoryManageService _inventoryManageService;
        private readonly IProductReservationService _productReservationService;
        private readonly IAuctionService _auctionService;
        private readonly IDiscountService _discountService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransactionService;

        public CancelOrderCommandHandler(
            IMediator mediator,
            IOrderService orderService,
            IShipmentService shipmentService,
            IProductService productService,
            IInventoryManageService inventoryManageService,
            IProductReservationService productReservationService,
            IAuctionService auctionService,
            IDiscountService discountService,
            IPaymentService paymentService,
            IPaymentTransactionService paymentTransactionService)
        {
            _mediator = mediator;
            _orderService = orderService;
            _shipmentService = shipmentService;
            _productService = productService;
            _inventoryManageService = inventoryManageService;
            _productReservationService = productReservationService;
            _auctionService = auctionService;
            _discountService = discountService;
            _paymentService = paymentService;
            _paymentTransactionService = paymentTransactionService;
        }

        public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            if (request.Order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
                throw new Exception("Cannot do cancel for order.");

            var shipments = await _shipmentService.GetShipmentsByOrder(request.Order.Id);
            if (shipments.Any())
                throw new Exception("Cannot do cancel for order with shipments");

            //Cancel order
            await _mediator.Send(new SetOrderStatusCommand()
            {
                Order = request.Order,
                Os = OrderStatusSystem.Cancelled,
                NotifyCustomer = request.NotifyCustomer,
                NotifyStoreOwner = request.NotifyStoreOwner
            });

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = "Order has been cancelled",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = request.Order.Id,

            });

            //return (add) back redeemded loyalty points
            await _mediator.Send(new ReturnBackRedeemedLoyaltyPointsCommand() { Order = request.Order });

            //Adjust inventory
            foreach (var orderItem in request.Order.OrderItems)
            {
                var product = await _productService.GetProductById(orderItem.ProductId);
                await _inventoryManageService.AdjustReserved(product, orderItem.Quantity - orderItem.CancelQty, orderItem.Attributes, orderItem.WarehouseId);
            }
            //update open qty
            foreach (var orderItem in request.Order.OrderItems)
            {
                orderItem.OpenQty = 0;
            }

            await _orderService.UpdateOrder(request.Order);

            //cancel reservations
            await _productReservationService.CancelReservationsByOrderId(request.Order.Id);

            //cancel bid
            await _auctionService.CancelBidByOrder(request.Order.Id);

            //cancel discount
            await _discountService.CancelDiscount(request.Order.Id);

            //cancel payments
            var payment = await _paymentTransactionService.GetByOrdeGuid(request.Order.OrderGuid);
            if (payment != null)
                await _paymentService.CancelPayment(payment);

            //event notification
            await _mediator.Publish(new OrderCancelledEvent(request.Order));

            return true;
        }
    }
}
