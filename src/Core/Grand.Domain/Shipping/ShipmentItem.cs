using Grand.Domain.Catalog;
using Grand.Domain.Audit;
using Grand.Domain.Common;

namespace Grand.Domain.Shipping;

/// <summary>
///     Represents a shipment item
/// </summary>
public class ShipmentItem : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the order item identifier
    /// </summary>
    public string OrderItemId { get; set; }

    /// <summary>
    ///     Gets or sets the product
    /// </summary>
    [AuditReference(typeof(Product))]
    [AuditName]
    public string ProductId { get; set; }

    /// <summary>
    ///     Gets or sets the product attribute xml
    /// </summary>
    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

    /// <summary>
    ///     Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    ///     Gets or sets the warehouse identifier
    /// </summary>
    [AuditReference(typeof(Warehouse))]
    public string WarehouseId { get; set; }

    /// <summary>
    ///     A batch-tracked product: the batches shipped; the rest of the quantity is stock without a batch
    /// </summary>
    public IList<ShipmentItemBatch> Batches { get; set; } = new List<ShipmentItemBatch>();

    /// <summary>
    ///     A serial-tracked product: the serial numbers shipped
    /// </summary>
    public IList<string> SerialNumbers { get; set; } = new List<string>();

    /// <summary>
    ///     The end of the warranty: the shipment's creation date plus the product's warranty months at that moment
    /// </summary>
    public DateTime? WarrantyUntilUtc { get; set; }

    /// <summary>
    ///     The units named were taken by another shipment between the check and the booking: neither the stock nor
    ///     the units of the item were booked
    /// </summary>
    public bool UnitsNotBooked { get; set; }
}