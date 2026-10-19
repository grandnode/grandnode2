namespace Grand.Business.Catalog.Queries.Handlers;

/// <summary>
///     The text index product keyword search uses when DatabaseConfig.UseProductTextSearch is on
/// </summary>
public static class ProductTextIndex
{
    public const string Name = "Product_Text";

    public static readonly string[] Fields = ["Name", "Sku", "Locales.LocaleValue"];
}
