﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class PartiallyRefundOfflineCommandHandler : IRequestHandler<PartiallyRefundOfflineCommand, bool>
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IMediator _mediator;
        private readonly IMessageProviderService _messageProviderService;
        private readonly LanguageSettings _languageSettings;

        public PartiallyRefundOfflineCommandHandler(
            IOrderService orderService,
            IPaymentTransactionService paymentTransactionService,
            IMediator mediator,
            IMessageProviderService messageProviderService,
            LanguageSettings languageSettings)
        {
            _orderService = orderService;
            _paymentTransactionService = paymentTransactionService;
            _mediator = mediator;
            _messageProviderService = messageProviderService;
            _languageSettings = languageSettings;
            _orderService = orderService;
        }

        public async Task<bool> Handle(PartiallyRefundOfflineCommand command, CancellationToken cancellationToken)
        {
            var paymentTransaction = command.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(command.PaymentTransaction));

            var amountToRefund = command.AmountToRefund;

            var canPartiallyRefundOffline = await _mediator.Send(new CanPartiallyRefundOfflineQuery { PaymentTransaction = paymentTransaction, AmountToRefund = amountToRefund }, cancellationToken);
            if (!canPartiallyRefundOffline)
                throw new GrandException("You can't partially refund (offline) this order");

            paymentTransaction.RefundedAmount += amountToRefund;
            paymentTransaction.TransactionStatus = paymentTransaction.RefundedAmount >= paymentTransaction.TransactionAmount ? TransactionStatus.Refunded : TransactionStatus.PartiallyRefunded;
            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //total amount refunded
            var totalAmountRefunded = order.RefundedAmount + amountToRefund;

            //update order info
            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatusId = order.RefundedAmount >= order.OrderTotal ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
            await _orderService.UpdateOrder(order);


            //check order status
            await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);

            //notifications
            _ = await _messageProviderService.SendOrderRefundedStoreOwnerMessage(order, amountToRefund, _languageSettings.DefaultAdminLanguageId);

            _ = await _messageProviderService.SendOrderRefundedCustomerMessage(order, amountToRefund, order.CustomerLanguageId);

            //raise event       
            await _mediator.Publish(new PaymentTransactionRefundedEvent(paymentTransaction, amountToRefund), cancellationToken);
            return true;
        }
    }
}
