using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class PartiallyPaidOfflineCommandHandler : IRequestHandler<PartiallyPaidOfflineCommand, bool>
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IMediator _mediator;

        public PartiallyPaidOfflineCommandHandler(
            IOrderService orderService,
            IPaymentTransactionService paymentTransactionService,
            IMediator mediator)
        {
            _orderService = orderService;
            _paymentTransactionService = paymentTransactionService;
            _mediator = mediator;
            _orderService = orderService;
        }

        public async Task<bool> Handle(PartiallyPaidOfflineCommand command, CancellationToken cancellationToken)
        {
            var paymentTransaction = command.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(command.PaymentTransaction));

            var amountToPaid = command.AmountToPaid;

            var canPartiallyPaidOffline = await _mediator.Send(new CanPartiallyPaidOfflineQuery() { PaymentTransaction = paymentTransaction, AmountToPaid = amountToPaid });
            if (!canPartiallyPaidOffline)
                throw new GrandException("You can't partially paid (offline) this transaction");

            paymentTransaction.PaidAmount += amountToPaid;
            paymentTransaction.TransactionStatus = paymentTransaction.PaidAmount >= paymentTransaction.TransactionAmount ? TransactionStatus.Paid : TransactionStatus.PartialPaid;
            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //update order info
            order.PaidAmount += amountToPaid;
            order.PaymentStatusId = order.PaidAmount >= order.OrderTotal ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
            await _orderService.UpdateOrder(order);

            //check order status
            await _mediator.Send(new CheckOrderStatusCommand() { Order = order });

            if (order.PaymentStatusId == PaymentStatus.Paid)
            {
                await _mediator.Send(new ProcessOrderPaidCommand() { Order = order });
            }

            return true;
        }
    }
}
