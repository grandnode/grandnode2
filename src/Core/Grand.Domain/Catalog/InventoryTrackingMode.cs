namespace Grand.Domain.Catalog;

/// <summary>
///     Which units of a product the inventory knows: none, batches or serial numbers, never both
/// </summary>
public enum InventoryTrackingMode
{
    None = 0,
    Batch = 10,
    SerialNumber = 20
}
