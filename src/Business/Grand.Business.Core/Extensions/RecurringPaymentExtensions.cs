using Grand.Domain.Orders;

namespace Grand.Business.Core.Extensions;

public static class RecurringPaymentExtensions
{
    /// <summary>
    ///     Whether a recurring payment in this status has ended: a Cancelled or Completed one charges nothing more and
    ///     cannot be cancelled
    /// </summary>
    public static bool HasEnded(this RecurringPaymentStatus status)
    {
        return status is RecurringPaymentStatus.Cancelled or RecurringPaymentStatus.Completed;
    }
}
