using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
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

        var claim = await recurringPaymentService.ClaimCycle(request.RecurringPaymentId, request.ProviderReference,
            request.CycleReference, request.Amount, request.PaidOnUtc, request.NextPaymentDateUtc);
        if (claim == null)
            return false;

        // the first cycle is the initial order's own payment, which the provider marks paid on its existing path
        if (claim.Cycle.CycleNumber > 1)
            try
            {
                await mediator.Send(new PlaceRecurringCycleOrderCommand {
                    RecurringPayment = claim.RecurringPayment, Cycle = claim.Cycle
                }, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Recurring payment {RecurringPaymentId}: the order of cycle {CycleNumber} ({CycleReference}) could not be created",
                    claim.RecurringPayment.Id, claim.Cycle.CycleNumber, request.CycleReference);
                await AddNote(claim.RecurringPayment,
                    $"Cycle {claim.Cycle.CycleNumber} was paid at the payment gateway ({request.CycleReference}), but its order could not be created: {e.Message}");
                // the failure must reach the provider so it can log or report it; the claim is already
                // recorded, so a resend of the same cycle is a no-op and does not retry the order
                throw;
            }

        if (claim.Completed)
        {
            IEnumerable<string> errors;
            try
            {
                var result = await paymentService.CancelRecurringPayment(claim.RecurringPayment);
                errors = result.Success ? [] : result.Errors;
            }
            catch (Exception e)
            {
                // a throw is treated as a refusal: the claim is recorded and a resend is a no-op, so the note is the only trace
                logger.LogError(e, "Recurring payment {RecurringPaymentId}: the payment gateway could not be asked to stop charging",
                    claim.RecurringPayment.Id);
                errors = [e.Message];
            }

            // the status stays Completed: every cycle is paid, only the gateway schedule may still run
            if (errors.Any())
                await AddNote(claim.RecurringPayment,
                    $"The recurring payment is completed, but the payment gateway did not confirm it stopped charging: {string.Join(", ", errors)}");
        }

        return true;
    }

    private async Task AddNote(RecurringPayment recurringPayment, string note)
    {
        try
        {
            await orderService.InsertOrderNote(new OrderNote {
                OrderId = recurringPayment.InitialOrderId, Note = note, DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
        }
        catch (Exception e)
        {
            // the note is a courtesy to staff; losing it must not hide the original failure or fail a recorded cycle
            logger.LogError(e, "Recurring payment {RecurringPaymentId}: the order note could not be written: {Note}",
                recurringPayment.Id, note);
        }
    }
}
