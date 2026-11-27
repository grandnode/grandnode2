using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     A payment provider reports that a recurring payment ended at the gateway. The result is true when it was
///     cancelled
/// </summary>
public class RecordRecurringPaymentEndedCommand : IRequest<bool>
{
    public required string RecurringPaymentId { get; set; }

    /// <summary>
    ///     The provider's reference of what ended (for example the subscription id), so the end of another
    ///     subscription started for the same order does not cancel the one the recurring payment runs on
    /// </summary>
    public string? ProviderReference { get; set; }
}
