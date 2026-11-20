using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     A payment provider reports that a recurring payment ended at the gateway. The result is true when it was
///     cancelled
/// </summary>
public class RecordRecurringPaymentEndedCommand : IRequest<bool>
{
    public string RecurringPaymentId { get; set; }
}
