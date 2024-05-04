using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Payments;
using Grand.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class CaptureCommandHandler : IRequestHandler<CaptureCommand, IList<string>>
{
    private readonly ILogger<CaptureCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentTransactionService _paymentTransaction;

    public CaptureCommandHandler(
        IPaymentService paymentService,
        IPaymentTransactionService paymentTransaction,
        IMediator mediator,
        IOrderService orderService,
        ILogger<CaptureCommandHandler> logger)
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

        var canCapture = await _mediator.Send(new CanCaptureQuery { PaymentTransaction = paymentTransaction },
            cancellationToken);
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
                    await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);
                    if (order.PaymentStatusId == PaymentStatus.Paid)
                        await _mediator.Send(new ProcessOrderPaidCommand { Order = order }, cancellationToken);
                }
            }
        }
        catch (Exception exc)
        {
            result ??= new CapturePaymentResult();
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

        if (!string.IsNullOrEmpty(error))
            //log it
            _logger.LogError("Error capturing order code # {PaymentTransactionOrderCode}. Error: {Error}",
                paymentTransaction.OrderCode, error);
        return result.Errors;
    }
}