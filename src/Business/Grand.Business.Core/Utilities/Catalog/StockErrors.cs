namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Why a stock change was refused; each is the key of a translation resource
/// </summary>
public static class StockErrors
{
    public const string ProductNotFound = "Admin.Inventory.Errors.ProductNotFound";
    public const string StockNotTracked = "Admin.Inventory.Errors.StockNotTracked";
    public const string WarehouseRequired = "Admin.Inventory.Errors.WarehouseRequired";
    public const string CombinationNotFound = "Admin.Inventory.Errors.CombinationNotFound";
    public const string NotEnoughAvailable = "Admin.Inventory.Errors.NotEnoughAvailable";

    /// <summary>
    ///     A change with this idempotency key was already applied; not an error, but a distinct outcome of the stock
    ///     integration API
    /// </summary>
    public const string AlreadyApplied = "Admin.Inventory.Errors.AlreadyApplied";

    public const string SkuNotFound = "Admin.Inventory.Errors.SkuNotFound";
    public const string SkuAmbiguous = "Admin.Inventory.Errors.SkuAmbiguous";
    public const string WarehouseCodeNotFound = "Admin.Inventory.Errors.WarehouseCodeNotFound";
    public const string WarehouseCodeAmbiguous = "Admin.Inventory.Errors.WarehouseCodeAmbiguous";
    public const string WarehouseNotOnProduct = "Admin.Inventory.Errors.WarehouseNotOnProduct";
    public const string TrackedSetNotSupported = "Admin.Inventory.Errors.TrackedSetNotSupported";

    public const string KeyRequired = "Admin.Inventory.Errors.KeyRequired";
    public const string KeyTooLong = "Admin.Inventory.Errors.KeyTooLong";
    public const string ProductTargetRequired = "Admin.Inventory.Errors.ProductTargetRequired";
    public const string ProductTargetBoth = "Admin.Inventory.Errors.ProductTargetBoth";
    public const string WarehouseTargetBoth = "Admin.Inventory.Errors.WarehouseTargetBoth";
    public const string ModeInvalid = "Admin.Inventory.Errors.ModeInvalid";
}
