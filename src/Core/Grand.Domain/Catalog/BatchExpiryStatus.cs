namespace Grand.Domain.Catalog;

/// <summary>
///     The expiry of a batch at a date; a batch without an expiry date has none
/// </summary>
public enum BatchExpiryStatus
{
    Valid = 10,
    Expiring = 20,
    Expired = 30
}
