using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
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

        if (await recurringPaymentService.RecordFailure(request.RecurringPaymentId, request.ProviderReference,
                request.CycleReference, request.Reason))
            return true;

        // a failed charge on a payment the shop ended means the gateway still tries to charge, typically a payment
        // cancelled while pending, before the gateway's reference was known
        var recurringPayment = await recurringPaymentService.GetRecurringPaymentById(request.RecurringPaymentId);
        if (recurringPayment != null && recurringPayment.HasOtherProviderReference(request.ProviderReference))
        {
            await RecurringPaymentGateway.NoteOtherReference(orderService, logger, recurringPayment,
                request.ProviderReference);
            return false;
        }

        if (recurringPayment?.Status is RecurringPaymentStatus.Cancelled or RecurringPaymentStatus.Completed &&
            recurringPayment.Cycles.All(x => x.ProviderReference != request.CycleReference))
        {
            if (string.IsNullOrEmpty(recurringPayment.ProviderReference))
                recurringPayment.ProviderReference = request.ProviderReference;
            await RecurringPaymentGateway.Stop(paymentService, orderService, logger, recurringPayment);
        }

        return false;
    }
}
