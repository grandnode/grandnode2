using Grand.Business.Checkout.Extensions;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using Grand.Mediator;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class MarkAsPaidCommandHandler : IRequestHandler<MarkAsPaidCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IPaymentTransactionService _paymentTransactionService;

    public MarkAsPaidCommandHandler(
        IMediator mediator,
        IOrderService orderService,
        IPaymentTransactionService paymentTransactionService)
    {
        _mediator = mediator;
        _orderService = orderService;
        _paymentTransactionService = paymentTransactionService;
    }

    public async Task<bool> Handle(MarkAsPaidCommand request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        var canMarkOrderAsPaid =
            await _mediator.Send(new CanMarkPaymentTransactionAsPaidQuery { PaymentTransaction = paymentTransaction },
                cancellationToken);
        if (!canMarkOrderAsPaid)
            throw new GrandException("You can't mark this Payment Transaction as paid");

        var entry = paymentTransaction.AddEntry(PaymentTransactionEntryType.Payment,
            paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount,
            request.CreatedBy, request.Reference, request.Note);
        paymentTransaction.TransactionStatus = TransactionStatus.Paid;
        paymentTransaction.PaidAmount = paymentTransaction.TransactionAmount;

        await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
        var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
        ArgumentNullException.ThrowIfNull(order);

        order.PaidAmount = paymentTransaction.PaidAmount;
        order.PaymentStatusId = PaymentStatus.Paid;
        order.PaidDateUtc = DateTime.UtcNow;
        await _orderService.UpdateOrder(order);

        await _orderService.InsertOrderNote(new OrderNote {
            Note = entry.ToOrderNote(paymentTransaction.CurrencyCode),
            DisplayToCustomer = false,
            OrderId = order.Id
        });

        await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);
        if (order.PaymentStatusId == PaymentStatus.Paid)
            await _mediator.Send(new ProcessOrderPaidCommand { Order = order }, cancellationToken);
        return true;
    }
}