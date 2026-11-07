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
}
