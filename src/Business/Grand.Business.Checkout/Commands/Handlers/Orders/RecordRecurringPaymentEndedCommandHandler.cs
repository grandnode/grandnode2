using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class RecordRecurringPaymentEndedCommandHandler(IRecurringPaymentService recurringPaymentService)
    : IRequestHandler<RecordRecurringPaymentEndedCommand, bool>
{
    public async Task<bool> Handle(RecordRecurringPaymentEndedCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.RecurringPaymentId);

        return await recurringPaymentService.RecordEnded(request.RecurringPaymentId);
    }
}
