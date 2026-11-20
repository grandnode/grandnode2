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
                // the failure must reach the provider, so the gateway resends and an operator sees it
                throw;
            }

        if (claim.Completed)
        {
            var result = await paymentService.CancelRecurringPayment(claim.RecurringPayment);
            // the status stays Completed: every cycle is paid, only the gateway schedule may still run
            if (!result.Success)
                await AddNote(claim.RecurringPayment,
                    $"The recurring payment is completed, but the payment gateway did not confirm it stopped charging: {string.Join(", ", result.Errors)}");
        }

        return true;
    }

    private Task AddNote(RecurringPayment recurringPayment, string note)
    {
        return orderService.InsertOrderNote(new OrderNote {
            OrderId = recurringPayment.InitialOrderId, Note = note, DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
    }
}
