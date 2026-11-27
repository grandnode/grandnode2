using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     A gateway report on a recurring payment, as the recurring payment service handled it under the lock
/// </summary>
public class RecurringReportResult
{
    public RecurringReportOutcome Outcome { get; init; }

    /// <summary>
    ///     The recurring payment as read under the lock, or as saved; null when it is unknown
    /// </summary>
    public RecurringPayment? RecurringPayment { get; init; }
}
