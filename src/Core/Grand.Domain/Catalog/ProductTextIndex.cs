namespace Grand.Domain.Catalog;

/// <summary>
///     The text index product keyword search uses when DatabaseConfig.UseProductTextSearch is on. The installer
///     and the 2.5 migration create it when the switch is on; search scans as before while it is missing. A
///     collection holds one text index, so changing <see cref="Fields" /> takes a new <see cref="Name" /> and a
///     migration that drops the previous index
/// </summary>
public static class ProductTextIndex
{
    public const string Name = "Product_Text";

    public static readonly string[] Fields = ["Name", "Sku", "Locales.LocaleValue"];
}
