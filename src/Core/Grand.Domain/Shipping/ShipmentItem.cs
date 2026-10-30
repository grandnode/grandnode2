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
}