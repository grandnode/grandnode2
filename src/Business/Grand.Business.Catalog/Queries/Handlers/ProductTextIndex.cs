namespace Grand.Business.Catalog.Queries.Handlers;

/// <summary>
///     The text index product keyword search uses when DatabaseConfig.UseProductTextSearch is on. A collection
///     holds one text index, so changing <see cref="Fields" /> takes a new <see cref="Name" /> and a migration that
///     drops the previous index; until then the new index is not created and search scans as before
/// </summary>
public static class ProductTextIndex
{
    public const string Name = "Product_Text";

    public static readonly string[] Fields = ["Name", "Sku", "Locales.LocaleValue"];
}
