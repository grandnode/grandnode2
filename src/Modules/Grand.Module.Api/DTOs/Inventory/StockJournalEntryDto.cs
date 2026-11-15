using Grand.Domain.Catalog;

namespace Grand.Module.Api.DTOs.Inventory;

/// <summary>
///     One inventory journal entry of the stock feed
/// </summary>
public class StockJournalEntryDto
{
    public string Id { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public string ProductId { get; set; }

    /// <summary>
    ///     The SKU of the product, or of the combination the entry names
    /// </summary>
    public string Sku { get; set; }

    public string CombinationId { get; set; }
    public string WarehouseId { get; set; }
    public string WarehouseCode { get; set; }
    public int InQty { get; set; }
    public int OutQty { get; set; }
    public InventoryMovementType MovementType { get; set; }
    public string ObjectType { get; set; }
    public string ObjectId { get; set; }
    public string Reference { get; set; }
    public string Comments { get; set; }
    public string CustomerId { get; set; }
    public string StoreId { get; set; }
    public string BatchNumber { get; set; }
    public IList<string> SerialNumbers { get; set; } = new List<string>();
    public DateTime? ExpiryDateUtc { get; set; }
    public string IdempotencyKey { get; set; }
}
