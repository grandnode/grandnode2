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
}
