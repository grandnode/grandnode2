using Grand.Business.Checkout.Extensions;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class MarkAsAuthorizedCommandHandler : IRequestHandler<MarkAsAuthorizedCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IPaymentTransactionService _paymentTransactionService;

    public MarkAsAuthorizedCommandHandler(
        IOrderService orderService,
        IPaymentTransactionService paymentTransactionService,
        IMediator mediator)
    {
        _orderService = orderService;
        _paymentTransactionService = paymentTransactionService;
        _mediator = mediator;
    }

    public async Task<bool> Handle(MarkAsAuthorizedCommand request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        var entry = paymentTransaction.AddEntry(PaymentTransactionEntryType.Authorization,
            paymentTransaction.TransactionAmount, request.CreatedBy);
        paymentTransaction.TransactionStatus = TransactionStatus.Authorized;
        await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

        var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
        ArgumentNullException.ThrowIfNull(order);

        order.PaymentStatusId = PaymentStatus.Authorized;
        await _orderService.UpdateOrder(order);
        await _orderService.InsertOrderNote(new OrderNote {
            Note = entry.ToOrderNote(paymentTransaction.CurrencyCode),
            DisplayToCustomer = false,
            OrderId = order.Id
        });

        //event notification
        await _mediator.Publish(new PaymentTransactionMarkAsAuthorizedEvent(paymentTransaction), cancellationToken);

        //check order status
        await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);

        return true;
    }
}