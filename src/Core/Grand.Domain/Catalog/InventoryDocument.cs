namespace Grand.Domain.Catalog;

/// <summary>
///     A receipt, transfer, adjustment, stock count or return receipt of one warehouse; a draft until posted, then
///     it never changes and a mistake is fixed by another document
/// </summary>
public class InventoryDocument : BaseEntity
{
    /// <summary>
    ///     The number people read: prefix, year and sequence, e.g. PZ/2026/0001
    /// </summary>
    public string Number { get; set; }

    public int Year { get; set; }

    public int Sequence { get; set; }

    public InventoryDocumentType DocumentTypeId { get; set; }

    public InventoryDocumentStatus StatusId { get; set; }

    public string WarehouseId { get; set; }

    /// <summary>
    ///     A transfer: the warehouse the stock moves to
    /// </summary>
    public string TargetWarehouseId { get; set; }

    /// <summary>
    ///     The store of the warehouse
    /// </summary>
    public string StoreId { get; set; }

    public IList<InventoryDocumentLine> Lines { get; set; } = new List<InventoryDocumentLine>();

    /// <summary>
    ///     A receipt: the supplier's name
    /// </summary>
    public string SupplierName { get; set; }

    /// <summary>
    ///     A receipt: the supplier's invoice or delivery note number
    /// </summary>
    public string ExternalReference { get; set; }

    /// <summary>
    ///     A return receipt: the merchandise return it receives
    /// </summary>
    public string MerchandiseReturnId { get; set; }

    public string Comment { get; set; }

    public DateTime? PostedOnUtc { get; set; }

    public string PostedBy { get; set; }
}
