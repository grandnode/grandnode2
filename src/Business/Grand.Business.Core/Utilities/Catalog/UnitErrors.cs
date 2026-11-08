namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Why a change of batches or serial numbers was refused; each is the key of a translation resource
/// </summary>
public static class UnitErrors
{
    public const string BatchRequired = "Admin.Inventory.Units.Errors.BatchRequired";
    public const string BatchNotFound = "Admin.Inventory.Units.Errors.BatchNotFound";
    public const string BatchQuantityExceeded = "Admin.Inventory.Units.Errors.BatchQuantityExceeded";
    public const string BatchExpiryMismatch = "Admin.Inventory.Units.Errors.BatchExpiryMismatch";
    public const string SerialRequired = "Admin.Inventory.Units.Errors.SerialRequired";
    public const string SerialCountMismatch = "Admin.Inventory.Units.Errors.SerialCountMismatch";
    public const string SerialDuplicate = "Admin.Inventory.Units.Errors.SerialDuplicate";
    public const string SerialInStock = "Admin.Inventory.Units.Errors.SerialInStock";
    public const string SerialShipped = "Admin.Inventory.Units.Errors.SerialShipped";
    public const string SerialNotInStock = "Admin.Inventory.Units.Errors.SerialNotInStock";
    public const string SerialInOtherWarehouse = "Admin.Inventory.Units.Errors.SerialInOtherWarehouse";
    public const string SerialNotShipped = "Admin.Inventory.Units.Errors.SerialNotShipped";
    public const string PoolExceeded = "Admin.Inventory.Units.Errors.PoolExceeded";

    /// <summary>
    ///     A change of tracking, inventory method or warehouses while units are assigned; the resource takes the
    ///     assigned quantity as {0}
    /// </summary>
    public const string UnitsAssigned = "Admin.Inventory.Units.Errors.UnitsAssigned";

    public const string TrackedStockDocumentsOnly = "Admin.Inventory.Units.Errors.TrackedStockDocumentsOnly";
    public const string TrackedBundleChild = "Admin.Inventory.Units.Errors.TrackedBundleChild";
    public const string TrackingNotSupported = "Admin.Inventory.Units.Errors.TrackingNotSupported";
}
