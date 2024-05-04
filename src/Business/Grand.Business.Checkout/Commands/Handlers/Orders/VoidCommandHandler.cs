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

public class VoidCommandHandler : IRequestHandler<VoidCommand, IList<string>>
{
    private readonly ILogger<VoidCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentTransactionService _paymentTransactionService;

    public VoidCommandHandler(
        IOrderService orderService,
        IMediator mediator,
        IPaymentService paymentService,
        IPaymentTransactionService paymentTransactionService,
        ILogger<VoidCommandHandler> logger)
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

        var canVoid = await _mediator.Send(new CanVoidQuery { PaymentTransaction = paymentTransaction },
            cancellationToken);
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

                if (paymentTransaction.TransactionStatus == TransactionStatus.Voided)
                {
                    order.PaymentStatusId = PaymentStatus.Voided;
                    await _orderService.UpdateOrder(order);

                    //check order status
                    await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);
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

        _logger.LogError("Error voiding order #{PaymentTransactionOrderCode}. Error: {Error}",
            paymentTransaction.OrderCode, error);
        return result.Errors;
    }
}