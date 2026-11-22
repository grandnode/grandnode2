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

        switch (report.Outcome)
        {
            case RecurringReportOutcome.Recorded:
                return true;
            case RecurringReportOutcome.OtherReference:
                await RecurringPaymentGateway.StopOtherReference(paymentService, orderService, logger,
                    report.RecurringPayment, request.ProviderReference,
                    $"a failed cycle ({request.CycleReference})");
                return false;
            case RecurringReportOutcome.Ended:
                // a failed charge on a payment the shop ended means the gateway still tries to charge, typically a
                // payment cancelled while pending; the service recorded the reported reference so it can be stopped
                await RecurringPaymentGateway.Stop(paymentService, orderService, logger, report.RecurringPayment);
                return false;
            default:
                return false;
        }
    }
}
