using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     A paid cycle recorded on a recurring payment
/// </summary>
public class RecurringCycleClaim
{
    /// <summary>
    ///     The recurring payment as saved
    /// </summary>
    public RecurringPayment RecurringPayment { get; init; }

    /// <summary>
    ///     The cycle that was recorded
    /// </summary>
    public RecurringPaymentCycle Cycle { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this cycle completed the recurring payment
    /// </summary>
    public bool Completed { get; init; }
}
