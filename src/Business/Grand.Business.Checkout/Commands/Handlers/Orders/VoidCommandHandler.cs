﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Domain.Logging;
using Grand.SharedKernel;
using MediatR;
using Grand.Business.Core.Extensions;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class VoidCommandHandler : IRequestHandler<VoidCommand, IList<string>>
    {
        private readonly IOrderService _orderService;
        private readonly IMediator _mediator;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly ILogger _logger;

        public VoidCommandHandler(
            IOrderService orderService,
            IMediator mediator,
            IPaymentService paymentService,
            IPaymentTransactionService paymentTransactionService,
            ILogger logger)
        {
            _orderService = orderService;
            _mediator = mediator;
            _paymentService = paymentService;
            _paymentTransactionService = paymentTransactionService;
            _logger = logger;
        }
        public async Task<IList<string>> Handle(VoidCommand command, CancellationToken cancellationToken)
        {
            var paymentTransaction = command.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(command.PaymentTransaction));

            var canVoid = await _mediator.Send(new CanVoidQuery { PaymentTransaction = paymentTransaction }, cancellationToken);
            if (!canVoid)
                throw new GrandException("Cannot do void for order.");

            VoidPaymentResult result = null;
            try
            {
                result = await _paymentService.Void(paymentTransaction);

                //event notification
                await _mediator.VoidPaymentTransactionDetailsEvent(result, paymentTransaction);

                if (result.Success)
                {
                    //update order info
                    paymentTransaction.TransactionStatus = result.NewTransactionStatus;
                    await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);

                    var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
                    if (order == null)
                        throw new ArgumentNullException(nameof(order));

                    if (paymentTransaction.TransactionStatus == Domain.Payments.TransactionStatus.Voided)
                    {
                        order.PaymentStatusId = Domain.Payments.PaymentStatus.Voided;
                        await _orderService.UpdateOrder(order);

                        //check order status
                        await _mediator.Send(new CheckOrderStatusCommand() { Order = order }, cancellationToken);
                    }
                }
            }
            catch (Exception exc)
            {
                result ??= new VoidPaymentResult();
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
            var logError = $"Error voiding order #{paymentTransaction.OrderCode}. Error: {error}";
            await _logger.InsertLog(LogLevel.Error, logError, logError);
            return result.Errors;
        }
    }
}
