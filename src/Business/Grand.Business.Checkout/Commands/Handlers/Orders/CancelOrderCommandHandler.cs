using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IAuctionService _auctionService;
    private readonly IDiscountService _discountService;
    private readonly IInventoryManageService _inventoryManageService;
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly IProductReservationService _productReservationService;
    private readonly IProductService _productService;
    private readonly IShipmentService _shipmentService;

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
        await _mediator.Send(new SetOrderStatusCommand {
            Order = request.Order,
            Os = OrderStatusSystem.Cancelled,
            NotifyCustomer = request.NotifyCustomer,
            NotifyStoreOwner = request.NotifyStoreOwner
        }, cancellationToken);

        //add a note
        await _orderService.InsertOrderNote(new OrderNote {
            Note = "Order has been cancelled",
            DisplayToCustomer = false,
            OrderId = request.Order.Id
        });

        //return (add) back redeemed loyalty points
        await _mediator.Send(new ReturnBackRedeemedLoyaltyPointsCommand { Order = request.Order }, cancellationToken);

        //Adjust inventory
        foreach (var orderItem in request.Order.OrderItems)
        {
            var product = await _productService.GetProductById(orderItem.ProductId);
            await _inventoryManageService.AdjustReserved(product, orderItem.Quantity - orderItem.CancelQty,
                orderItem.Attributes, orderItem.WarehouseId);
        }

        //update open qty
        foreach (var orderItem in request.Order.OrderItems) orderItem.OpenQty = 0;

        await _orderService.UpdateOrder(request.Order);

        //cancel reservations
        await _productReservationService.CancelReservationsByOrderId(request.Order.Id);

        //cancel bid
        await _auctionService.CancelBidByOrder(request.Order.Id);

        //cancel discount
        await _discountService.CancelDiscount(request.Order.Id);

        //cancel payments
        var payment = await _paymentTransactionService.GetOrderByGuid(request.Order.OrderGuid);
        if (payment != null)
            await _paymentService.CancelPayment(payment);

        //event notification
        await _mediator.Publish(new OrderCancelledEvent(request.Order), cancellationToken);

        return true;
    }
}