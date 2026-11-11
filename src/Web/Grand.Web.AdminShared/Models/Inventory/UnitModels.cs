using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.Inventory;

public class SerialNumberListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.SerialNumber")]
    public string SearchNumber { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.Status")]
    public int? SearchStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Warehouse")]
    public string SearchWarehouseId { get; set; }

    /// <summary>
    ///     The serial numbers of one product, from its edit page
    /// </summary>
    public string ProductId { get; set; }

    public IList<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
}

public class SerialNumberRowModel
{
    public string Id { get; set; }
    public string Number { get; set; }
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public string Status { get; set; }
    public string Warehouse { get; set; }
    public string OrderId { get; set; }
    public DateTime? ShippedOn { get; set; }
    public DateTime? WarrantyUntil { get; set; }
}

public class SerialNumberModel : SerialNumberRowModel
{
    public DateTime ReceivedOn { get; set; }
    public string ReceiptDocumentId { get; set; }
    public string ShipmentId { get; set; }
    public bool InWarranty { get; set; }

    //what the back office tells about the unit, all optional

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.Location")]
    public string LocationId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.ManufacturedOn")]
    public DateTime? ManufacturedOn { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.Supplier")]
    public string Supplier { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.SupplierWarrantyUntil")]
    public DateTime? SupplierWarrantyUntil { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.Imei")]
    public string Imei { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.MacAddress")]
    public string MacAddress { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.FirmwareVersion")]
    public string FirmwareVersion { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.AdminComment")]
    public string AdminComment { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.CustomerComment")]
    public string CustomerComment { get; set; }

    public IList<SelectListItem> AvailableLocations { get; set; } = new List<SelectListItem>();
}

public class BatchListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.BatchNumber")]
    public string SearchNumber { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Warehouse")]
    public string SearchWarehouseId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.ExpiryStatus")]
    public int? SearchExpiryStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.WithStockOnly")]
    public bool WithStockOnly { get; set; } = true;

    /// <summary>
    ///     The batches of one product, from its edit page
    /// </summary>
    public string ProductId { get; set; }

    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableExpiryStatuses { get; set; } = new List<SelectListItem>();
}

public class BatchRowModel
{
    public string Id { get; set; }
    public string Number { get; set; }
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public string Warehouse { get; set; }
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    ///     The expiry status translated; empty without an expiry date
    /// </summary>
    public string ExpiryStatus { get; set; }

    public int Quantity { get; set; }
}

public class BatchModel : BatchRowModel
{
    public decimal? UnitCost { get; set; }
    public DateTime ReceivedOn { get; set; }
    public string ReceiptDocumentId { get; set; }

    //what the back office tells about the batch, all optional

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.Location")]
    public string LocationId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.ManufacturedOn")]
    public DateTime? ManufacturedOn { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.Supplier")]
    public string Supplier { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.SupplierBatchNumber")]
    public string SupplierBatchNumber { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.CertificateNumber")]
    public string CertificateNumber { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.AdminComment")]
    public string AdminComment { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Units.Fields.CustomerComment")]
    public string CustomerComment { get; set; }

    public IList<SelectListItem> AvailableLocations { get; set; } = new List<SelectListItem>();
}

public class BatchRecipientModel
{
    public string ShipmentId { get; set; }
    public int ShipmentNumber { get; set; }
    public string OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string CustomerId { get; set; }
    public string CustomerEmail { get; set; }
    public int Quantity { get; set; }
    public DateTime ShippedOn { get; set; }
}

/// <summary>
///     One stock bucket on the product's batches or serial numbers tab
/// </summary>
public class UnitBucketRowModel
{
    public string Warehouse { get; set; }
    public string Combination { get; set; }
    public int Stock { get; set; }
    public int Assigned { get; set; }
    public int Pool { get; set; }

    /// <summary>
    ///     The batches holding stock in the bucket, earliest expiry first, or the first of its serial numbers in stock
    /// </summary>
    public IList<UnitInBucketModel> Units { get; set; } = new List<UnitInBucketModel>();

    /// <summary>
    ///     The serial numbers in stock not listed in <see cref="Units" />
    /// </summary>
    public int MoreUnits => Math.Max(0, Assigned - Units.Sum(x => x.Quantity));
}

/// <summary>
///     A batch or a serial number in a bucket on the product's batches or serial numbers tab
/// </summary>
public class UnitInBucketModel
{
    public string Id { get; set; }
    public string Number { get; set; }
    public int Quantity { get; set; }

    /// <summary>
    ///     A date without time
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    public BatchExpiryStatus? ExpiryStatus { get; set; }

    /// <summary>
    ///     The code of the place in the warehouse where it is kept
    /// </summary>
    public string Location { get; set; }

    public DateTime? ReceivedOn { get; set; }
}
