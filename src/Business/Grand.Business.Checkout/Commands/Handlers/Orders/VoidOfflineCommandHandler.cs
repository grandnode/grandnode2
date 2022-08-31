using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using MediatR;

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
