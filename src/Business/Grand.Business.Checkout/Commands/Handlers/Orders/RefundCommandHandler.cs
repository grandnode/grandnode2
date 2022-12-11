using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Domain.Logging;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class RefundCommandHandler : IRequestHandler<RefundCommand, IList<string>>
    {
        private readonly IMediator _mediator;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IOrderService _orderService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly ILogger _logger;
        private readonly LanguageSettings _languageSettings;

        public RefundCommandHandler(
            IMediator mediator,
            IPaymentService paymentService,
            IPaymentTransactionService paymentTransactionService,
            IOrderService orderService,
            IMessageProviderService messageProviderService,
            ILogger logger,
            LanguageSettings languageSettings)
        {
            _mediator = mediator;
            _paymentService = paymentService;
            _paymentTransactionService = paymentTransactionService;
            _orderService = orderService;
            _messageProviderService = messageProviderService;
            _logger = logger;
            _languageSettings = languageSettings;
        }


        public async Task<IList<string>> Handle(RefundCommand command, CancellationToken cancellationToken)
        {
            var paymentTransaction = command.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(command.PaymentTransaction));

            //if (!await CanRefund(order))
            //    throw new GrandException("Cannot do refund for order.");

            var request = new RefundPaymentRequest();
            RefundPaymentResult result = null;
            try
            {
                request.PaymentTransaction = paymentTransaction;
                request.AmountToRefund = paymentTransaction.PaidAmount;
                request.IsPartialRefund = false;
                result = await _paymentService.Refund(request);
                if (result.Success)
                {
                    paymentTransaction.TransactionStatus = result.NewTransactionStatus;
                    paymentTransaction.RefundedAmount += request.AmountToRefund;
                    await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

                    var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
                    if (order == null)
                        throw new ArgumentNullException(nameof(order));

                    var totalAmountRefunded = order.RefundedAmount + request.AmountToRefund;

                    //update order info
                    order.RefundedAmount = totalAmountRefunded;
                    order.PaymentStatusId = order.RefundedAmount == order.OrderTotal ? Domain.Payments.PaymentStatus.Refunded : Domain.Payments.PaymentStatus.PartiallyRefunded;
                    await _orderService.UpdateOrder(order);

                    //check order status
                    await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);

                    //notifications for store owner
                    await _messageProviderService.SendOrderRefundedStoreOwnerMessage(order, request.AmountToRefund, _languageSettings.DefaultAdminLanguageId);

                    //notifications for customer
                    await _messageProviderService.SendOrderRefundedCustomerMessage(order, request.AmountToRefund, order.CustomerLanguageId);

                    //raise event       
                    await _mediator.Publish(new PaymentTransactionRefundedEvent(paymentTransaction, request.AmountToRefund), cancellationToken);
                }

            }
            catch (Exception exc)
            {
                result ??= new RefundPaymentResult();
                result.AddError($"Error: {exc.Message}. Full exception: {exc}");
            }

            //process errors
            var error = "";
            for (var i = 0; i < result.Errors.Count; i++)
            {
                error += $"Error {i}: {result.Errors[i]}";
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }

            if (string.IsNullOrEmpty(error)) return result.Errors;
            //log it
            var logError = $"Error refunding order #{paymentTransaction.OrderCode}. Error: {error}";
            await _logger.InsertLog(LogLevel.Error, logError, logError);
            return result.Errors;
        }
    }
}
