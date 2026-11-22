using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class RecordRecurringPaymentEndedCommandHandler(
    IRecurringPaymentService recurringPaymentService,
    IOrderService orderService,
    ILogger<RecordRecurringPaymentEndedCommandHandler> logger)
    : IRequestHandler<RecordRecurringPaymentEndedCommand, bool>
{
    public async Task<bool> Handle(RecordRecurringPaymentEndedCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.RecurringPaymentId);

        if (await recurringPaymentService.RecordEnded(request.RecurringPaymentId, request.ProviderReference))
            return true;

        var recurringPayment = await recurringPaymentService.GetRecurringPaymentById(request.RecurringPaymentId);
        if (recurringPayment != null && recurringPayment.HasOtherProviderReference(request.ProviderReference))
            await RecurringPaymentGateway.NoteOtherReference(orderService, logger, recurringPayment,
                request.ProviderReference);

        return false;
    }
}
