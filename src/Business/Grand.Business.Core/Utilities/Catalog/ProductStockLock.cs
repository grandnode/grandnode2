namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     The resource lock key every writer of a product's stock holds
/// </summary>
public static class ProductStockLock
{
    public static string Key(string productId)
    {
        return $"Product:{productId}";
    }
}
