using Grand.Domain.Catalog;

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Whether a batch is valid, expiring or expired at a date. Expiry never blocks anything: it is shown and reported
/// </summary>
public static class BatchExpiry
{
    /// <summary>
    ///     The status of a batch on the day <paramref name="today" />; null for a batch without an expiry date
    /// </summary>
    /// <param name="expiryDateUtc">The expiry date; its time is ignored</param>
    /// <param name="today">The date in the store's time zone</param>
    /// <param name="warningDays">How many days before its expiry date a batch is expiring</param>
    public static BatchExpiryStatus? Status(DateTime? expiryDateUtc, DateTime today, int warningDays)
    {
        if (!expiryDateUtc.HasValue)
            return null;

        var expiry = expiryDateUtc.Value.Date;
        if (expiry < today.Date)
            return BatchExpiryStatus.Expired;
        return expiry <= today.Date.AddDays(warningDays) ? BatchExpiryStatus.Expiring : BatchExpiryStatus.Valid;
    }

    /// <summary>
    ///     The days before its expiry date a batch of the product is expiring
    /// </summary>
    public static int WarningDays(Product product, InventorySettings settings)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(settings);

        return product.ExpiryWarningDays ?? settings.ExpiryWarningDays;
    }
}
