using Grand.Domain.Catalog;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     The words for a recurring payment's cycle: "Every month" for one, "Every 3 months" for more; the enum
///     resources are plural, so "Every {0} {1}" reads "Every 1 Months"
/// </summary>
public static class RecurringCycleText
{
    /// <summary>
    ///     Gets the resource key of the cycle; the resource takes the cycle length as {0}
    /// </summary>
    public static string ResourceKey(int cycleLength, RecurringCyclePeriod period)
    {
        var (one, many) = period switch {
            RecurringCyclePeriod.Weeks => ("Week", "Weeks"),
            RecurringCyclePeriod.Months => ("Month", "Months"),
            RecurringCyclePeriod.Years => ("Year", "Years"),
            _ => ("Day", "Days")
        };
        return $"RecurringPayment.Cycle.{(cycleLength == 1 ? one : many)}";
    }
}
