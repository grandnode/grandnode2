using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class RecordRecurringPaymentReferenceCommandHandler(
    IRecurringPaymentService recurringPaymentService,
    IPaymentService paymentService,
    IOrderService orderService,
    ILogger<RecordRecurringPaymentReferenceCommandHandler> logger)
    : IRequestHandler<RecordRecurringPaymentReferenceCommand, bool>
{
    public async Task<bool> Handle(RecordRecurringPaymentReferenceCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.RecurringPaymentId);
        ArgumentException.ThrowIfNullOrEmpty(request.ProviderReference);

        var report = await recurringPaymentService.RecordProviderReference(request.RecurringPaymentId,
            request.ProviderReference);

        switch (report.Outcome)
        {
            case RecurringReportOutcome.Recorded:
                // cancelled before the reference was known, so the cancel could not stop the gateway: it is stopped
                // now, once, as a resend finds the reference recorded
                if (report.RecurringPayment.Status.HasEnded())
                    await RecurringPaymentGateway.Stop(paymentService, orderService, logger, report.RecurringPayment);
                return true;
            case RecurringReportOutcome.Duplicate:
                return true;
            case RecurringReportOutcome.OtherReference:
                await RecurringPaymentGateway.StopOtherReference(paymentService, orderService, logger,
                    report.RecurringPayment, request.ProviderReference, "a second subscription");
                return false;
            default:
                return false;
        }
    }
}
