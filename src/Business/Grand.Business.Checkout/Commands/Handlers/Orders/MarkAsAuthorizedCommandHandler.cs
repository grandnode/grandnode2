﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class MarkAsAuthorizedCommandHandler : IRequestHandler<MarkAsAuthorizedCommand, bool>
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IMediator _mediator;

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
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            paymentTransaction.TransactionStatus = TransactionStatus.Authorized;
            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            order.PaymentStatusId = PaymentStatus.Authorized;
            await _orderService.UpdateOrder(order);

            //event notification
            await _mediator.Publish(new PaymentTransactionMarkAsAuthorizedEvent(paymentTransaction), cancellationToken);

            //check order status
            await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);

            return true;
        }
    }
}
