using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class RecordRecurringPaymentFailureCommandHandler(IRecurringPaymentService recurringPaymentService)
    : IRequestHandler<RecordRecurringPaymentFailureCommand, bool>
{
    public async Task<bool> Handle(RecordRecurringPaymentFailureCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.RecurringPaymentId);

        return await recurringPaymentService.RecordFailure(request.RecurringPaymentId, request.CycleReference,
            request.Reason);
    }
}
