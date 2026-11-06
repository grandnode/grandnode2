namespace Grand.Domain.Catalog;

/// <summary>
///     The kind of an inventory document
/// </summary>
public enum InventoryDocumentType
{
    Receipt = 10,
    Transfer = 20,
    Adjustment = 30,
    StockCount = 40,
    ReturnReceipt = 50
}
