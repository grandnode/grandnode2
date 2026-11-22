using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class RecordRecurringPaymentEndedCommandHandler(
    IRecurringPaymentService recurringPaymentService,
    ILogger<RecordRecurringPaymentEndedCommandHandler> logger)
    : IRequestHandler<RecordRecurringPaymentEndedCommand, bool>
{
    public async Task<bool> Handle(RecordRecurringPaymentEndedCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.RecurringPaymentId);

        var report = await recurringPaymentService.RecordEnded(request.RecurringPaymentId, request.ProviderReference);

        // another subscription that ended charges nothing more, typically one the shop stopped: nothing for staff to do
        if (report.Outcome == RecurringReportOutcome.OtherReference)
            logger.LogInformation("Recurring payment {RecurringPaymentId} runs on {ProviderReference}; {ReportedReference} ended at the payment gateway",
                report.RecurringPayment.Id, report.RecurringPayment.ProviderReference, request.ProviderReference);

        return report.Outcome == RecurringReportOutcome.Recorded;
    }
}
