using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class VoidOfflineCommandHandler : IRequestHandler<VoidOfflineCommand, bool>
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IMediator _mediator;

        public VoidOfflineCommandHandler(
            IOrderService orderService,
            IPaymentTransactionService paymentTransactionService,
            IMediator mediator)
        {
            _orderService = orderService;
            _paymentTransactionService = paymentTransactionService;
            _mediator = mediator;
        }

        public async Task<bool> Handle(VoidOfflineCommand request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            paymentTransaction.TransactionStatus = TransactionStatus.Voided;
            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (!(await _mediator.Send(new CanVoidOfflineQuery() { PaymentTransaction = paymentTransaction })))
                throw new GrandException("You can't void this order");

            order.PaymentStatusId = PaymentStatus.Voided;
            await _orderService.UpdateOrder(order);

            //event notification
            await _mediator.Publish(new PaymentTransactionVoidOfflineEvent(paymentTransaction));

            //check orer status
            await _mediator.Send(new CheckOrderStatusCommand() { Order = order });
            return true;
        }
    }
}
