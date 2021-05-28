using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Localization;
using Grand.Domain.Logging;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

                    double totalAmountRefunded = order.RefundedAmount + request.AmountToRefund;

                    //update order info
                    order.RefundedAmount = totalAmountRefunded;
                    order.PaymentStatusId = order.RefundedAmount == order.OrderTotal ? Domain.Payments.PaymentStatus.Refunded : Domain.Payments.PaymentStatus.PartiallyRefunded;
                    await _orderService.UpdateOrder(order);

                    //check order status
                    await _mediator.Send(new CheckOrderStatusCommand() { Order = order });

                    //notifications
                    var orderRefundedStoreOwnerNotificationQueuedEmailId = await _messageProviderService.SendOrderRefundedStoreOwnerMessage(order, request.AmountToRefund, _languageSettings.DefaultAdminLanguageId);
                    if (orderRefundedStoreOwnerNotificationQueuedEmailId > 0)
                    {
                        await _orderService.InsertOrderNote(new OrderNote
                        {
                            Note = "Order refunded email (to store owner) has been queued.",
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow,
                            OrderId = order.Id,
                        });
                    }

                    //notifications
                    var orderRefundedCustomerNotificationQueuedEmailId = await _messageProviderService.SendOrderRefundedCustomerMessage(order, request.AmountToRefund, order.CustomerLanguageId);
                    if (orderRefundedCustomerNotificationQueuedEmailId > 0)
                    {
                        await _orderService.InsertOrderNote(new OrderNote
                        {
                            Note = "Order refunded email (to customer) has been queued.",
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow,
                            OrderId = order.Id,
                        });
                    }

                    //raise event       
                    await _mediator.Publish(new PaymentTransactionRefundedEvent(paymentTransaction, request.AmountToRefund));
                }

            }
            catch (Exception exc)
            {
                if (result == null)
                    result = new RefundPaymentResult();
                result.AddError(string.Format("Error: {0}. Full exception: {1}", exc.Message, exc.ToString()));
            }

            //process errors
            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!String.IsNullOrEmpty(error))
            {

                //log it
                string logError = string.Format("Error refunding order #{0}. Error: {1}", paymentTransaction.OrderCode, error);
                await _logger.InsertLog(LogLevel.Error, logError, logError);
            }
            return result.Errors;
        }
    }
}
