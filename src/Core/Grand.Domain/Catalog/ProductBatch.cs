using Grand.Domain.Common;

namespace Grand.Domain.Catalog;

/// <summary>
///     The quantity of one batch of a product, or of one combination of it, in one stock bucket. A record whose
///     quantity reaches 0 is kept, for history and recalls
/// </summary>
public class ProductBatch : BaseEntity
{
    public string ProductId { get; set; }

    /// <summary>
    ///     The combination of a product stocked by attributes
    /// </summary>
    public string CombinationId { get; set; }

    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

    /// <summary>
    ///     The warehouse of the bucket, as the inventory journal names it; empty for a product without one
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    ///     The store of the warehouse; empty for a shared warehouse
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     The batch number as entered
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    ///     The number trimmed and upper-cased: what lookups and the unique index compare
    /// </summary>
    public string NumberKey { get; set; }

    /// <summary>
    ///     The expiry date, a date without time
    /// </summary>
    public DateTime? ExpiryDateUtc { get; set; }

    /// <summary>
    ///     The purchase price of one unit, in the primary store currency
    /// </summary>
    public decimal? UnitCost { get; set; }

    public int Quantity { get; set; }

    public DateTime ReceivedOnUtc { get; set; }

    /// <summary>
    ///     The receipt that brought the batch in
    /// </summary>
    public string ReceiptDocumentId { get; set; }

    /// <summary>
    ///     The stock revision of the last inventory journal entry that changed the record; an entry not newer than
    ///     it is already applied
    /// </summary>
    public int StockRevision { get; set; }

    /// <summary>
    ///     The expiry status the store owner was last e-mailed about
    /// </summary>
    public BatchExpiryStatus? ExpiryNotifiedStatusId { get; set; }
}
