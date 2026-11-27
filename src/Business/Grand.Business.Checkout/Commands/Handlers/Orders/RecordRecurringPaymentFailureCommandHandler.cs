using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class RecordRecurringPaymentFailureCommandHandler(
    IRecurringPaymentService recurringPaymentService,
    IPaymentService paymentService,
    IOrderService orderService,
    ILogger<RecordRecurringPaymentFailureCommandHandler> logger)
    : IRequestHandler<RecordRecurringPaymentFailureCommand, bool>
{
    public async Task<bool> Handle(RecordRecurringPaymentFailureCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.RecurringPaymentId);
        ArgumentException.ThrowIfNullOrEmpty(request.CycleReference);

        var report = await recurringPaymentService.RecordFailure(request.RecurringPaymentId,
            request.ProviderReference, request.CycleReference, request.Reason);

        //a payment is reported with every outcome but NotFound
        switch (report)
        {
            case { Outcome: RecurringReportOutcome.Recorded }:
                return true;
            case { Outcome: RecurringReportOutcome.OtherReference, RecurringPayment: { } otherPayment }:
                await RecurringPaymentGateway.StopOtherReference(paymentService, orderService, logger,
                    otherPayment, request.ProviderReference,
                    $"a failed cycle ({request.CycleReference})");
                return false;
            case { Outcome: RecurringReportOutcome.Ended, RecurringPayment: { } endedPayment }:
                // a failed charge on a payment the shop ended means the gateway still tries to charge, typically a
                // payment cancelled while pending; the service recorded the reported reference so it can be stopped
                await RecurringPaymentGateway.Stop(paymentService, orderService, logger, endedPayment);
                return false;
            default:
                return false;
        }
    }
}
