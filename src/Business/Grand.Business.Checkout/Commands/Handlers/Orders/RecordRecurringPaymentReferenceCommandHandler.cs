using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
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

        var recurringPayment = await recurringPaymentService.RecordProviderReference(request.RecurringPaymentId,
            request.ProviderReference);
        if (recurringPayment == null)
            return false;

        if (recurringPayment.HasOtherProviderReference(request.ProviderReference))
        {
            await RecurringPaymentGateway.NoteOtherReference(orderService, logger, recurringPayment,
                request.ProviderReference);
            return false;
        }

        // cancelled before the reference was known, so the cancel could not stop the gateway: it is stopped now
        if (recurringPayment.Status.HasEnded())
            await RecurringPaymentGateway.Stop(paymentService, orderService, logger, recurringPayment);

        return true;
    }
}
