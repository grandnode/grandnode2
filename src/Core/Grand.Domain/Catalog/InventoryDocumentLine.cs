using Grand.Domain.Common;

namespace Grand.Domain.Catalog;

/// <summary>
///     One product, or one combination of it, on an inventory document
/// </summary>
public class InventoryDocumentLine : SubBaseEntity
{
    public string ProductId { get; set; }

    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

    /// <summary>
    ///     The quantity received, moved or returned; signed on an adjustment
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    ///     A stock count: the stock stored when the count was created
    /// </summary>
    public int? ExpectedQuantity { get; set; }

    /// <summary>
    ///     A stock count: the quantity counted; a line left empty is not booked
    /// </summary>
    public int? CountedQuantity { get; set; }

    /// <summary>
    ///     A receipt: the purchase price of one unit, in the primary store currency
    /// </summary>
    public decimal? UnitCost { get; set; }

    /// <summary>
    ///     A return receipt: whether the item goes back on stock
    /// </summary>
    public ReturnLineCondition Condition { get; set; }

    /// <summary>
    ///     A return receipt: the merchandise return item the line receives
    /// </summary>
    public string MerchandiseReturnItemId { get; set; }

    public string Comment { get; set; }

    /// <summary>
    ///     A batch-tracked product: the batch the line books; empty for the stock without a batch
    /// </summary>
    public string BatchNumber { get; set; }

    /// <summary>
    ///     A receipt of a batch: its expiry date
    /// </summary>
    public DateTime? ExpiryDateUtc { get; set; }

    /// <summary>
    ///     A serial-tracked product: the serial numbers the line books
    /// </summary>
    public IList<string> SerialNumbers { get; set; } = new List<string>();
}
