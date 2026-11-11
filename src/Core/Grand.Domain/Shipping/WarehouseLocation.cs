using Grand.Domain.Audit;

namespace Grand.Domain.Shipping;

/// <summary>
///     A place in a warehouse where units are kept: a zone, rack, shelf or bin, known by its code
/// </summary>
public class WarehouseLocation : BaseEntity
{
    [AuditReference(typeof(Warehouse))]
    public string WarehouseId { get; set; }

    /// <summary>
    ///     The code written on the place, such as A-01-03
    /// </summary>
    [AuditName]
    public string Code { get; set; }

    public string Name { get; set; }

    public int DisplayOrder { get; set; }
}
