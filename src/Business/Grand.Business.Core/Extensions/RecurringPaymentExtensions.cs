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

    /// <summary>
    ///     Whether a gateway report names another reference than the one the recurring payment runs on, for example a
    ///     second subscription started for the same order. False while either reference is unknown
    /// </summary>
    public static bool HasOtherProviderReference(this RecurringPayment recurringPayment, string providerReference)
    {
        return !string.IsNullOrEmpty(recurringPayment.ProviderReference) && !string.IsNullOrEmpty(providerReference) &&
               recurringPayment.ProviderReference != providerReference;
    }
}
