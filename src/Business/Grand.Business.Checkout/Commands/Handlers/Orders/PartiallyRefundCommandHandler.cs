﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Domain.Logging;
using Grand.SharedKernel;
using MediatR;

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

            var canPartiallyRefund = await _mediator.Send(new CanPartiallyRefundQuery { AmountToRefund = amountToRefund, PaymentTransaction = paymentTransaction }, cancellationToken);
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
                        var totalAmountRefunded = order.RefundedAmount + amountToRefund;

                        order.RefundedAmount = totalAmountRefunded;
                        order.PaymentStatusId = order.RefundedAmount == order.OrderTotal ? Domain.Payments.PaymentStatus.Refunded : Domain.Payments.PaymentStatus.PartiallyRefunded;
                        await _orderService.UpdateOrder(order);

                        //check order status
                        await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);

                        //notifications
                        await _messageProviderService.SendOrderRefundedStoreOwnerMessage(order, amountToRefund, _languageSettings.DefaultAdminLanguageId);

                        await _messageProviderService.SendOrderRefundedCustomerMessage(order, amountToRefund, order.CustomerLanguageId);

                    }
                    //raise event       
                    await _mediator.Publish(new PaymentTransactionRefundedEvent(paymentTransaction, amountToRefund), cancellationToken);
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
            var logError = $"Error refunding order #{paymentTransaction.OrderCode}. Error: {error}";
            await _logger.InsertLog(LogLevel.Error, logError, logError);
            return result.Errors;
        }
    }
}
