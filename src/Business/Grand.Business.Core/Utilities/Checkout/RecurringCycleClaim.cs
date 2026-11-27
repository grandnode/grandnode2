using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     A paid cycle reported on a recurring payment
/// </summary>
public class RecurringCycleClaim : RecurringReportResult
{
    /// <summary>
    ///     The cycle that was recorded; null unless the outcome is Recorded
    /// </summary>
    public RecurringPaymentCycle? Cycle { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this cycle completed the recurring payment
    /// </summary>
    public bool Completed { get; init; }
}
