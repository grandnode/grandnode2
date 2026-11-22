using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     A payment provider reports that a cycle of a recurring payment was paid. The result is true when the cycle was
///     recorded now and false when there was nothing to do (an unknown recurring payment, a cycle already recorded, or
///     another subscription than the one the recurring payment runs on, which is asked to stop)
/// </summary>
public class RecordRecurringPaymentCycleCommand : IRequest<bool>
{
    /// <summary>
    ///     The recurring payment the provider was given as gateway metadata
    /// </summary>
    public string RecurringPaymentId { get; set; }

    /// <summary>
    ///     The provider's reference of the subscription
    /// </summary>
    public string ProviderReference { get; set; }

    /// <summary>
    ///     The provider's reference of the cycle (the invoice or charge), so a resent report is recorded once
    /// </summary>
    public string CycleReference { get; set; }

    /// <summary>
    ///     The amount paid, in the units of <see cref="MarkAsPaidCommand.Amount" />
    /// </summary>
    public double Amount { get; set; }

    public DateTime PaidOnUtc { get; set; }

    /// <summary>
    ///     The next payment date the gateway reports, if any
    /// </summary>
    public DateTime? NextPaymentDateUtc { get; set; }
}
