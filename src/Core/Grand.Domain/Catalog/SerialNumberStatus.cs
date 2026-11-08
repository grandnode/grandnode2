namespace Grand.Domain.Catalog;

/// <summary>
///     Where a unit with a serial number is
/// </summary>
public enum SerialNumberStatus
{
    InStock = 10,
    Shipped = 20,
    Scrapped = 30,
    Missing = 40
}
