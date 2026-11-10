using Grand.Domain.Common;

namespace Grand.Domain.Catalog;

/// <summary>
///     One unit of a product with a serial number: where it is and, once shipped, whom it was sold to
/// </summary>
public class ProductSerialNumber : BaseEntity
{
    public string ProductId { get; set; }

    /// <summary>
    ///     The combination of a product stocked by attributes
    /// </summary>
    public string CombinationId { get; set; }

    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

    /// <summary>
    ///     The serial number as entered
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    ///     The number trimmed and upper-cased: what lookups and the unique index compare
    /// </summary>
    public string NumberKey { get; set; }

    public SerialNumberStatus StatusId { get; set; }

    /// <summary>
    ///     The warehouse of the bucket the unit is or was last in, as the inventory journal names it
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    ///     The store of the warehouse; empty for a shared warehouse
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     The purchase price of the unit, in the primary store currency
    /// </summary>
    public decimal? UnitCost { get; set; }

    public DateTime ReceivedOnUtc { get; set; }

    /// <summary>
    ///     The receipt that brought the unit in
    /// </summary>
    public string ReceiptDocumentId { get; set; }

    public string OrderId { get; set; }

    public string OrderItemId { get; set; }

    public string ShipmentId { get; set; }

    public DateTime? ShippedOnUtc { get; set; }

    /// <summary>
    ///     The end of the warranty, fixed when the unit was shipped
    /// </summary>
    public DateTime? WarrantyUntilUtc { get; set; }

    /// <summary>
    ///     The stock revision of the last inventory journal entry that changed the record; an entry not newer than
    ///     it is already applied
    /// </summary>
    public int StockRevision { get; set; }

    //what the back office tells about the unit, all optional; the ledger never writes these

    /// <summary>
    ///     The place in the unit's warehouse where it is kept; a location of another warehouse is a stale one
    /// </summary>
    public string LocationId { get; set; }

    /// <summary>
    ///     The production date, a date without time
    /// </summary>
    public DateTime? ManufacturedOnUtc { get; set; }

    public string Supplier { get; set; }

    /// <summary>
    ///     The end of the warranty the supplier or manufacturer gives the store, a date without time
    /// </summary>
    public DateTime? SupplierWarrantyUntilUtc { get; set; }

    /// <summary>
    ///     The IMEI of a device with a mobile modem
    /// </summary>
    public string Imei { get; set; }

    public string MacAddress { get; set; }

    public string FirmwareVersion { get; set; }

    public string AdminComment { get; set; }

    /// <summary>
    ///     Shown to the customer who received the unit
    /// </summary>
    public string CustomerComment { get; set; }
}
