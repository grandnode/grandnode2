using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Domain.Logging;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class CaptureCommandHandler : IRequestHandler<CaptureCommand, IList<string>>
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransaction;
        private readonly IOrderService _orderService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CaptureCommandHandler(
            IPaymentService paymentService,
            IPaymentTransactionService paymentTransaction,
            IMediator mediator,
            IOrderService orderService,
            ILogger logger)
        {
            _paymentService = paymentService;
            _paymentTransaction = paymentTransaction;
            _mediator = mediator;
            _orderService = orderService;
            _logger = logger;
        }

        public async Task<IList<string>> Handle(CaptureCommand command, CancellationToken cancellationToken)
        {
            var paymentTransaction = command.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(command.PaymentTransaction));

            var canCapture = await _mediator.Send(new CanCaptureQuery() { PaymentTransaction = paymentTransaction });
            if (!canCapture)
                throw new GrandException("Cannot do capture for order.");

            CapturePaymentResult result = null;
            try
            {
                result = await _paymentService.Capture(paymentTransaction);

                //event notification
                await _mediator.CapturePaymentTransactionDetailsEvent(result, paymentTransaction);

                if (result.Success)
                {
                    paymentTransaction = await _paymentTransaction.GetById(paymentTransaction.Id);
                    paymentTransaction.PaidAmount = paymentTransaction.TransactionAmount;
                    paymentTransaction.CaptureTransactionId = result.CaptureTransactionId;
                    paymentTransaction.CaptureTransactionResult = result.CaptureTransactionResult;
                    paymentTransaction.TransactionStatus = result.NewPaymentStatus;

                    await _paymentTransaction.UpdatePaymentTransaction(paymentTransaction);

                    var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
                    if (order != null && paymentTransaction.TransactionStatus == TransactionStatus.Paid)
                    {
                        order.PaidAmount = paymentTransaction.PaidAmount;
                        order.PaymentStatusId = PaymentStatus.Paid;
                        order.PaidDateUtc = DateTime.UtcNow;
                        await _orderService.UpdateOrder(order);
                        await _mediator.Send(new CheckOrderStatusCommand() { Order = order });
                        if (order.PaymentStatusId == PaymentStatus.Paid)
                        {
                            await _mediator.Send(new ProcessOrderPaidCommand() { Order = order });
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                if (result == null)
                    result = new CapturePaymentResult();
                result.AddError(string.Format("Error: {0}. Full exception: {1}", exc.Message, exc));
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
                await _logger.InsertLog(LogLevel.Error, $"Error capturing order code # {paymentTransaction.OrderCode}. Error: {error}", $"Error capturing order code # {paymentTransaction.OrderCode}. Error: {error}");
            }
            return result.Errors;
        }
    }
}
