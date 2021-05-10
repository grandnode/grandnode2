using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Domain.Logging;
using Grand.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

            var canVoid = await _mediator.Send(new CanVoidQuery() { PaymentTransaction = paymentTransaction });
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
                        await _mediator.Send(new CheckOrderStatusCommand() { Order = order });
                    }
                }
            }
            catch (Exception exc)
            {
                if (result == null)
                    result = new VoidPaymentResult();
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
                string logError = string.Format("Error voiding order #{0}. Error: {1}", paymentTransaction.OrderCode, error);
                await _logger.InsertLog(LogLevel.Error, logError, logError);
            }
            return result.Errors;
        }
    }
}
