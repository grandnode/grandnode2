using Grand.Domain.Common;

namespace Grand.Domain.Catalog;

/// <summary>
///     One change of physical stock in one bucket; entries are only ever added, a reversal is a counter-entry
/// </summary>
public class InventoryJournal : BaseEntity
{
    public string ObjectId { get; set; }
    public string ObjectType { get; set; }
    public string PositionId { get; set; }
    public DateTime CreateDateUtc { get; set; }
    public string ProductId { get; set; }
    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();
    public string WarehouseId { get; set; }
    public int InQty { get; set; }
    public int OutQty { get; set; }
    public string Comments { get; set; }
    public string Reference { get; set; }

    /// <summary>
    ///     What caused the change
    /// </summary>
    public InventoryMovementType MovementTypeId { get; set; }

    /// <summary>
    ///     Who caused it: a staff member or an API user; empty for the system
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    ///     The store of the warehouse; empty for a shared warehouse or a product without one
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     The combination of a product stocked by attributes
    /// </summary>
    public string CombinationId { get; set; }

    /// <summary>
    ///     The product's stock revision this entry brought it to; an entry newer than the product's is applied
    ///     before the next change
    /// </summary>
    public int StockRevision { get; set; }
}
