using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Localization;
using Grand.Domain.Logging;
using Grand.Domain.Orders;
using Grand.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class PartiallyRefundCommandHandler : IRequestHandler<PartiallyRefundCommand, IList<string>>
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IMediator _mediator;
        private readonly IMessageProviderService _messageProviderService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly LanguageSettings _languageSettings;

        public PartiallyRefundCommandHandler(
            IPaymentService paymentService,
            IPaymentTransactionService paymentTransactionService,
            IMediator mediator,
            IMessageProviderService messageProviderService,
            ILogger logger,
            IOrderService orderService,
            LanguageSettings languageSettings)
        {
            _paymentService = paymentService;
            _paymentTransactionService = paymentTransactionService;
            _orderService = orderService;
            _mediator = mediator;
            _messageProviderService = messageProviderService;
            _logger = logger;
            _languageSettings = languageSettings;
        }

        public async Task<IList<string>> Handle(PartiallyRefundCommand command, CancellationToken cancellationToken)
        {
            var paymentTransaction = command.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(command.PaymentTransaction));

            var amountToRefund = command.AmountToRefund;

            var canPartiallyRefund = await _mediator.Send(new CanPartiallyRefundQuery() { AmountToRefund = amountToRefund, PaymentTransaction = paymentTransaction });
            if (!canPartiallyRefund)
                throw new GrandException("Cannot do partial refund for order.");

            var request = new RefundPaymentRequest();
            RefundPaymentResult result = null;
            try
            {
                request.PaymentTransaction = paymentTransaction;
                request.AmountToRefund = amountToRefund;
                request.IsPartialRefund = true;

                result = await _paymentService.Refund(request);

                if (result.Success)
                {
                    paymentTransaction = await _paymentTransactionService.GetById(paymentTransaction.Id);
                    paymentTransaction.TransactionStatus = result.NewTransactionStatus;
                    paymentTransaction.RefundedAmount += amountToRefund;
                    await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

                    var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
                    if (order == null)
                        throw new ArgumentNullException(nameof(order));

                    //total amount refunded
                    if (paymentTransaction.TransactionStatus == Domain.Payments.TransactionStatus.Refunded)
                    {
                        double totalAmountRefunded = order.RefundedAmount + amountToRefund;

                        order.RefundedAmount = totalAmountRefunded;
                        order.PaymentStatusId = order.RefundedAmount == order.OrderTotal ? Domain.Payments.PaymentStatus.Refunded : Domain.Payments.PaymentStatus.PartiallyRefunded;
                        await _orderService.UpdateOrder(order);

                        //check order status
                        await _mediator.Send(new CheckOrderStatusCommand() { Order = order });

                        //notifications
                        var orderRefundedStoreOwnerNotificationQueuedEmailId = await _messageProviderService.SendOrderRefundedStoreOwnerMessage(order, amountToRefund, _languageSettings.DefaultAdminLanguageId);
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


                        var orderRefundedCustomerNotificationQueuedEmailId = await _messageProviderService.SendOrderRefundedCustomerMessage(order, amountToRefund, order.CustomerLanguageId);
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
                    }
                    //raise event       
                    await _mediator.Publish(new PaymentTransactionRefundedEvent(paymentTransaction, amountToRefund));
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

                string logError = string.Format("Error refunding order #{0}. Error: {1}", paymentTransaction.OrderCode, error);
                await _logger.InsertLog(LogLevel.Error, logError, logError);
            }
            return result.Errors;
        }
    }
}
