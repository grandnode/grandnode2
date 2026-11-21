using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class CancelRecurringPaymentCommandHandler(
    IRecurringPaymentService recurringPaymentService,
    IPaymentService paymentService,
    ILogger<CancelRecurringPaymentCommandHandler> logger)
    : IRequestHandler<CancelRecurringPaymentCommand, CancelRecurringPaymentResult>
{
    public async Task<CancelRecurringPaymentResult> Handle(CancelRecurringPaymentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.RecurringPayment);
        ArgumentException.ThrowIfNullOrEmpty(request.CancelledBy);

        return await recurringPaymentService.Cancel(request.RecurringPayment.Id, request.CancelledBy, StopAtGateway);
    }

    private async Task<CancelRecurringPaymentResult> StopAtGateway(RecurringPayment recurringPayment)
    {
        try
        {
            return await paymentService.CancelRecurringPayment(recurringPayment);
        }
        catch (Exception e)
        {
            //a throw is treated as a refusal: the gateway may still be charging, so the status must stay as it is
            logger.LogError(e, "Recurring payment {RecurringPaymentId}: the payment gateway could not be asked to stop charging",
                recurringPayment.Id);
            var result = new CancelRecurringPaymentResult();
            result.AddError(e.Message);
            return result;
        }
    }
}
