using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     What caused a stock change, as the inventory journal records it
/// </summary>
/// <param name="ObjectType">the source object's type: Shipment, InventoryDocument, Product, Api</param>
/// <param name="ObjectId">the source object's id</param>
/// <param name="PositionId">the source object's line</param>
/// <param name="Reference">a number people read: the shipment or document number</param>
/// <param name="CustomerId">who caused it; empty for the system</param>
/// <param name="Comment">the comment of the entry</param>
public sealed record StockSource(
    string ObjectType = "",
    string ObjectId = "",
    string PositionId = "",
    string Reference = "",
    string CustomerId = "",
    string Comment = "");

/// <summary>
///     A change of one stock bucket: the product, its combination, or either of them in one warehouse
/// </summary>
/// <param name="Attributes">the combination's attributes, for a product stocked by attributes</param>
/// <param name="WarehouseId">the warehouse, for a product with multiple warehouses</param>
/// <param name="Quantity">a signed difference; with <paramref name="SetTo" /> the quantity to end with</param>
/// <param name="SetTo">the quantity is a target: the difference to the stored stock is worked out under the lock</param>
/// <param name="MovementType">what caused the change</param>
/// <param name="Source">the source the journal entry names</param>
public sealed record StockChange(
    IList<CustomAttribute> Attributes,
    string WarehouseId,
    int Quantity,
    bool SetTo,
    InventoryMovementType MovementType,
    StockSource Source)
{
    /// <summary>
    ///     A reserved quantity to end with; reservations are order state, so it is written without a journal entry
    /// </summary>
    public int? ReservedQuantity { get; init; }

    /// <summary>
    ///     The purchase price of one unit received; only a receipt carries it
    /// </summary>
    public decimal? UnitCost { get; init; }

    /// <summary>
    ///     A batch-tracked product: the batch the change moves; empty for the stock without a batch. For a
    ///     <see cref="InventoryMovementType.TrackingAssignment" /> the quantity is what the batch takes from that stock
    /// </summary>
    public string BatchNumber { get; init; }

    /// <summary>
    ///     A batch received: its expiry date
    /// </summary>
    public DateTime? ExpiryDateUtc { get; init; }

    /// <summary>
    ///     A serial-tracked product: the serial numbers the change moves; the rest of the quantity is stock without them
    /// </summary>
    public IReadOnlyList<string> SerialNumbers { get; init; } = [];

    /// <summary>
    ///     A stock count of a serial-tracked bucket: every serial number found in it. The change is worked out under the
    ///     lock against the units stored then; null for any other change
    /// </summary>
    public IReadOnlyList<string> CountedSerialNumbers { get; init; }
}
