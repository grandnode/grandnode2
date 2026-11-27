using System.Runtime.ExceptionServices;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class RecordRecurringPaymentCycleCommandHandler(
    IRecurringPaymentService recurringPaymentService,
    IPaymentService paymentService,
    IOrderService orderService,
    IMediator mediator,
    ILogger<RecordRecurringPaymentCycleCommandHandler> logger)
    : IRequestHandler<RecordRecurringPaymentCycleCommand, bool>
{
    public async Task<bool> Handle(RecordRecurringPaymentCycleCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.RecurringPaymentId);
        ArgumentException.ThrowIfNullOrEmpty(request.CycleReference);
        if (double.IsNaN(request.Amount) || request.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "The cycle amount must be zero or more");

        var claim = await recurringPaymentService.ClaimCycle(request.RecurringPaymentId, request.ProviderReference,
            request.CycleReference, request.Amount, request.PaidOnUtc, request.NextPaymentDateUtc);
        if (claim is { Outcome: RecurringReportOutcome.OtherReference, RecurringPayment: { } otherPayment })
        {
            await RecurringPaymentGateway.StopOtherReference(paymentService, orderService, logger,
                otherPayment, request.ProviderReference,
                $"a paid cycle ({request.CycleReference})", "check whether the customer must be refunded");
            return false;
        }

        // a resend is the usual reason; a recorded claim carries its payment and cycle
        if (claim is not
            { Outcome: RecurringReportOutcome.Recorded, RecurringPayment: { } recurringPayment, Cycle: { } cycle })
            return false;

        // the cycle is recorded: an aborted webhook request must not leave it half done, so nothing below is cancellable
        cancellationToken = CancellationToken.None;

        // the first cycle is the initial order's own payment, which the provider marks paid on its existing path
        ExceptionDispatchInfo? placementFailure = null;
        if (cycle.CycleNumber > 1)
            try
            {
                await mediator.Send(new PlaceRecurringCycleOrderCommand {
                    RecurringPayment = recurringPayment, Cycle = cycle
                }, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Recurring payment {RecurringPaymentId}: the order {OrderId} of cycle {CycleNumber} ({CycleReference}) could not be placed or marked paid",
                    recurringPayment.Id, cycle.OrderId, cycle.CycleNumber, request.CycleReference);
                // the order may exist already (the failure can come after it was inserted), so the note names it
                await RecurringPaymentGateway.AddNote(orderService, logger, recurringPayment,
                    $"Cycle {cycle.CycleNumber} was paid at the payment gateway ({request.CycleReference}), but its order {cycle.OrderId} could not be placed or marked paid: {e.Message}");
                // the failure must reach the provider so it can log or report it, but only after the gateway was
                // asked to stop: the claim is recorded, so a resend is a no-op and would never stop it
                placementFailure = ExceptionDispatchInfo.Capture(e);
            }

        // a cycle claimed while the payment is already completed or cancelled means the gateway still charges
        if (recurringPayment.Status is RecurringPaymentStatus.Completed or RecurringPaymentStatus.Cancelled)
        {
            // the last cycle completing the payment is expected; any other was taken after the shop ended it
            if (!claim.Completed)
                await RecurringPaymentGateway.AddNote(orderService, logger, recurringPayment,
                    $"Cycle {cycle.CycleNumber} was charged at the payment gateway ({request.CycleReference}) after the recurring payment was {recurringPayment.Status.ToString().ToLowerInvariant()}: check whether the customer must be refunded");
            await RecurringPaymentGateway.Stop(paymentService, orderService, logger, recurringPayment);
        }

        placementFailure?.Throw();

        return true;
    }
}
