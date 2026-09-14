using Grand.Domain.Configuration;

namespace Grand.Domain.Directory;

/// <summary>
///     The currency a store's prices are stored in.
///     Kept apart from <see cref="CurrencySettings" /> so a single store can override it: settings fall back to the
///     global value per object rather than per field, so a store-scoped override placed on that class would also
///     freeze its system-wide fields - the exchange rate currency, the rate provider and the auto update flag - for
///     the same store.
/// </summary>
public class PrimaryCurrencySettings : ISettings
{
    /// <summary>
    ///     Gets or sets the identifier of the currency prices are stored in
    /// </summary>
    public string CurrencyId { get; set; }
}
