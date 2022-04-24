using Grand.Business.Core.Commands.Checkout.Orders;
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
    public class RefundOfflineCommandHandler : IRequestHandler<RefundOfflineCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly LanguageSettings _languageSettings;

        public RefundOfflineCommandHandler(
            IMediator mediator,
            IOrderService orderService,
            IPaymentTransactionService paymentTransactionService,
            IMessageProviderService messageProviderService,
            LanguageSettings languageSettings)
        {
            _mediator = mediator;
            _orderService = orderService;
            _paymentTransactionService = paymentTransactionService;
            _messageProviderService = messageProviderService;
            _languageSettings = languageSettings;
        }

        public async Task<bool> Handle(RefundOfflineCommand request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            var canRefundOffline = await _mediator.Send(new CanRefundOfflineQuery() { PaymentTransaction = paymentTransaction });
            if (!canRefundOffline)
                throw new GrandException("You can't refund this payment transaction");

            paymentTransaction.RefundedAmount += paymentTransaction.TransactionAmount;
            paymentTransaction.TransactionStatus = paymentTransaction.RefundedAmount >= paymentTransaction.TransactionAmount ? TransactionStatus.Refunded : TransactionStatus.PartiallyRefunded;
            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);


            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //amout to refund
            double amountToRefund = order.OrderTotal;

            //total amount refunded
            double totalAmountRefunded = order.RefundedAmount + amountToRefund;

            //update order info
            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatusId = order.RefundedAmount >= order.OrderTotal ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;

            await _orderService.UpdateOrder(order);


            //check order status
            await _mediator.Send(new CheckOrderStatusCommand() { Order = order });

            //notifications for store owner
            await _messageProviderService.SendOrderRefundedStoreOwnerMessage(order, amountToRefund, _languageSettings.DefaultAdminLanguageId);

            //notifications for customer
            await _messageProviderService.SendOrderRefundedCustomerMessage(order, amountToRefund, order.CustomerLanguageId);

            //raise event       
            await _mediator.Publish(new PaymentTransactionRefundedEvent(paymentTransaction, amountToRefund));
            return true;
        }
    }
}
