using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.Inventory;

public class InventoryDocumentListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Number")]
    public string SearchNumber { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.DocumentType")]
    public int? SearchDocumentTypeId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Status")]
    public int? SearchStatusId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Warehouse")]
    public string SearchWarehouseId { get; set; }

    public IList<SelectListItem> AvailableDocumentTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
}

/// <summary>
///     A row of the inventory document list
/// </summary>
public class InventoryDocumentRowModel : BaseEntityModel
{
    public string Number { get; set; }
    public string DocumentType { get; set; }
    public string Status { get; set; }
    public string Warehouse { get; set; }
    public int Lines { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class InventoryDocumentModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Number")]
    public string Number { get; set; }

    public int DocumentTypeId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.DocumentType")]
    public string DocumentType { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Status")]
    public string Status { get; set; }

    public bool Posted { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Warehouse")]
    public string WarehouseId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.TargetWarehouse")]
    public string TargetWarehouseId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.SupplierName")]
    public string SupplierName { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.ExternalReference")]
    public string ExternalReference { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Comment")]
    public string Comment { get; set; }

    /// <summary>
    ///     A stock count: only products of these categories
    /// </summary>
    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Categories")]
    public IList<string> CategoryIds { get; set; } = new List<string>();

    /// <summary>
    ///     A stock count: only products of these brands
    /// </summary>
    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Brands")]
    public IList<string> BrandIds { get; set; } = new List<string>();

    /// <summary>
    ///     A product to add, by its SKU; a product stocked by attributes adds a line per combination
    /// </summary>
    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.AddSku")]
    public string AddSku { get; set; }

    public IList<InventoryDocumentLineModel> Lines { get; set; } = new List<InventoryDocumentLineModel>();

    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCategories { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableBrands { get; set; } = new List<SelectListItem>();
}

public class InventoryDocumentLineModel : BaseEntityModel
{
    public string ProductName { get; set; }
    public string Sku { get; set; }
    public string AttributesText { get; set; }
    public int Quantity { get; set; }
    public int? ExpectedQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public int ConditionId { get; set; }
    public string Comment { get; set; }

    /// <summary>
    ///     The line is taken off the draft when it is saved
    /// </summary>
    public bool Remove { get; set; }
}

/// <summary>
///     A journal entry on the stock history tab of a product
/// </summary>
public class StockHistoryRowModel
{
    public DateTime Date { get; set; }
    public string MovementType { get; set; }
    public string Warehouse { get; set; }
    public string Combination { get; set; }
    public int In { get; set; }
    public int Out { get; set; }
    public string Reference { get; set; }
    public string DocumentId { get; set; }
    public string ShipmentId { get; set; }
    public string Who { get; set; }
    public string Comment { get; set; }
}

public class StockAtDateModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Inventory.Documents.Fields.Warehouse")]
    public string WarehouseId { get; set; }

    [GrandResourceDisplayName("Admin.Inventory.StockAtDate.Fields.AtDate")]
    public DateTime AtDate { get; set; }

    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();
}

/// <summary>
///     A bucket of the stock at date report; the stock is "-" for a date before its history starts
/// </summary>
public class StockAtDateRowModel
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public string Sku { get; set; }
    public string Combination { get; set; }
    public string Stock { get; set; }
}
