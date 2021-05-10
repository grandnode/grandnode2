using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
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
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            var canMarkOrderAsPaid = await _mediator.Send(new CanMarkPaymentTransactionAsPaidQuery() { PaymentTransaction = paymentTransaction });
            if (!canMarkOrderAsPaid)
                throw new GrandException("You can't mark this Payment Transaction as paid");

            paymentTransaction.TransactionStatus = TransactionStatus.Paid;
            paymentTransaction.PaidAmount = paymentTransaction.TransactionAmount;

            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            order.PaidAmount = paymentTransaction.PaidAmount;
            order.PaymentStatusId = PaymentStatus.Paid;
            order.PaidDateUtc = DateTime.UtcNow;
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = "Order has been marked as paid",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,

            });

            await _mediator.Send(new CheckOrderStatusCommand() { Order = order });
            if (order.PaymentStatusId == PaymentStatus.Paid)
            {
                await _mediator.Send(new ProcessOrderPaidCommand() { Order = order });
            }
            return true;
        }
    }
}
