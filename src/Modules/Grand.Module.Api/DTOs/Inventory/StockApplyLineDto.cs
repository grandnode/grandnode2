using Grand.Domain.Common;

namespace Grand.Module.Api.DTOs.Inventory;

/// <summary>
///     One line of a bulk stock write: a target "set to" or a difference "change by", with its idempotency key
/// </summary>
public class StockApplyLineDto
{
    /// <summary>
    ///     The idempotency key; required, at most 100 characters. A retried call with the same key is not applied again
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    ///     The product by id; exactly one of ProductId and Sku
    /// </summary>
    public string ProductId { get; set; }

    /// <summary>
    ///     The product or combination by SKU; exactly one of ProductId and Sku
    /// </summary>
    public string Sku { get; set; }

    /// <summary>
    ///     With ProductId, the attributes of a product stocked by attributes
    /// </summary>
    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

    /// <summary>
    ///     The warehouse by id; at most one of WarehouseId and WarehouseCode
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    ///     The warehouse by code; at most one of WarehouseId and WarehouseCode
    /// </summary>
    public string WarehouseCode { get; set; }

    /// <summary>
    ///     "Set" or "Change"
    /// </summary>
    public string Mode { get; set; }

    /// <summary>
    ///     The target for Set, the signed difference for Change
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    ///     A batch-tracked product: the batch the line moves
    /// </summary>
    public string BatchNumber { get; set; }

    /// <summary>
    ///     A batch received: its expiry date
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    ///     A serial-tracked product: the serial numbers the line moves
    /// </summary>
    public IList<string> SerialNumbers { get; set; } = new List<string>();

    /// <summary>
    ///     The reference of the journal entry; the SKU when empty
    /// </summary>
    public string Reference { get; set; }

    /// <summary>
    ///     The comment of the journal entry
    /// </summary>
    public string Comment { get; set; }
}
