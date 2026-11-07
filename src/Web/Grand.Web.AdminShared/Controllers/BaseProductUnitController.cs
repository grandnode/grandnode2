using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Inventory;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Text;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     Finds serial numbers and batches, and who received a batch. Store sees the units kept in its own warehouses
/// </summary>
[PermissionAuthorize(PermissionSystemName.InventoryDocuments)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductUnitController(
    IProductUnitService productUnitService,
    IProductService productService,
    IWarehouseService warehouseService,
    IDateTimeService dateTimeService,
    IEnumTranslationService enumTranslationService,
    IRequestTranslationService translationService,
    IInventoryDocumentViewModelService inventoryDocumentViewModelService,
    IAdminDataScope<Product> productScope) : BaseController
{
    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpGet]
    public async Task<IActionResult> SerialNumbers(string productId)
    {
        var model = new SerialNumberListModel { ProductId = productId };
        model.AvailableStatuses.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var status in Enum.GetValues<SerialNumberStatus>())
            model.AvailableStatuses.Add(new SelectListItem
                { Text = enumTranslationService.GetTranslationEnum(status), Value = ((int)status).ToString() });
        await AddWarehouses(model.AvailableWarehouses);
        return View("SerialNumbers", model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> SerialNumberList(DataSourceRequest command, SerialNumberListModel model)
    {
        var serialNumbers = await productUnitService.SearchSerialNumbers(new UnitSearch {
            Number = model.SearchNumber, Prefix = true, ProductId = model.ProductId,
            Status = (SerialNumberStatus?)model.SearchStatusId, WarehouseId = model.SearchWarehouseId,
            StoreId = await productScope.GetDefaultStoreIdAsync()
        }, command.Page - 1, command.PageSize);

        var rows = new List<SerialNumberRowModel>();
        foreach (var serialNumber in serialNumbers)
            rows.Add(await Row(serialNumber, new SerialNumberRowModel()));
        return Json(new DataSourceResult { Data = rows, Total = serialNumbers.TotalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpGet]
    public async Task<IActionResult> SerialNumber(string id)
    {
        var serialNumber = await productUnitService.GetSerialNumber(id);
        if (serialNumber == null || !await CanSee(serialNumber.StoreId))
            return Denied("SerialNumbers");

        var model = await Row(serialNumber, new SerialNumberModel());
        model.ReceivedOn = dateTimeService.ConvertToUserTime(serialNumber.ReceivedOnUtc, DateTimeKind.Utc);
        model.ReceiptDocumentId = serialNumber.ReceiptDocumentId;
        model.ShipmentId = serialNumber.ShipmentId;
        model.InWarranty = serialNumber.WarrantyUntilUtc > DateTime.UtcNow;
        model.History = await History(serialNumber.ProductId, null, serialNumber.Number);
        return View("SerialNumber", model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpGet]
    public async Task<IActionResult> Batches(string productId, int? expiryStatus)
    {
        //the expiring batches report is this list filtered by its expiry status
        var model = new BatchListModel { ProductId = productId, SearchExpiryStatusId = expiryStatus };
        model.AvailableExpiryStatuses.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var status in Enum.GetValues<BatchExpiryStatus>())
            model.AvailableExpiryStatuses.Add(new SelectListItem
                { Text = enumTranslationService.GetTranslationEnum(status), Value = ((int)status).ToString() });
        await AddWarehouses(model.AvailableWarehouses);
        return View("Batches", model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> BatchList(DataSourceRequest command, BatchListModel model)
    {
        var batches = await productUnitService.SearchBatches(new UnitSearch {
            Number = model.SearchNumber, Prefix = true, ProductId = model.ProductId,
            WarehouseId = model.SearchWarehouseId, ExpiryStatus = (BatchExpiryStatus?)model.SearchExpiryStatusId,
            WithStockOnly = model.WithStockOnly, StoreId = await productScope.GetDefaultStoreIdAsync(), Today = Today()
        }, command.Page - 1, command.PageSize);

        var rows = new List<BatchRowModel>();
        foreach (var batch in batches)
            rows.Add(await Row(batch, new BatchRowModel()));
        return Json(new DataSourceResult { Data = rows, Total = batches.TotalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpGet]
    public async Task<IActionResult> Batch(string id)
    {
        var batch = await productUnitService.GetBatch(id);
        if (batch == null || !await CanSee(batch.StoreId))
            return Denied("Batches");

        var model = await Row(batch, new BatchModel());
        model.UnitCost = batch.UnitCost;
        model.ReceivedOn = dateTimeService.ConvertToUserTime(batch.ReceivedOnUtc, DateTimeKind.Utc);
        model.ReceiptDocumentId = batch.ReceiptDocumentId;
        foreach (var recipient in await productUnitService.BatchRecipients(batch))
            model.Recipients.Add(new BatchRecipientModel {
                ShipmentId = recipient.ShipmentId, ShipmentNumber = recipient.ShipmentNumber,
                OrderId = recipient.OrderId, OrderNumber = recipient.OrderNumber, CustomerId = recipient.CustomerId,
                CustomerEmail = recipient.CustomerEmail, Quantity = recipient.Quantity,
                ShippedOn = dateTimeService.ConvertToUserTime(recipient.ShippedOnUtc, DateTimeKind.Utc)
            });
        model.History = await History(batch.ProductId, batch.Number, null);
        return View("Batch", model);
    }

    /// <summary>
    ///     Who received the batch, for a recall
    /// </summary>
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpGet]
    public async Task<IActionResult> BatchRecipientsCsv(string id)
    {
        var batch = await productUnitService.GetBatch(id);
        if (batch == null || !await CanSee(batch.StoreId))
            return Denied("Batches");

        var csv = new StringBuilder("Batch;Shipment;Order;Customer;Email;Quantity;Shipped\r\n");
        foreach (var recipient in await productUnitService.BatchRecipients(batch))
            csv.Append(string.Join(";", Csv(batch.Number), recipient.ShipmentNumber, recipient.OrderNumber,
                    Csv(recipient.CustomerId), Csv(recipient.CustomerEmail), recipient.Quantity,
                    recipient.ShippedOnUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
                .Append("\r\n");
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv", $"batch-{batch.Number}.csv");
    }

    /// <summary>
    ///     A CSV field, quoted, and never read as a formula by a spreadsheet
    /// </summary>
    private static string Csv(string value)
    {
        value ??= "";
        if (value.Length > 0 && "=+-@".Contains(value[0]))
            value = "'" + value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private async Task<bool> CanSee(string unitStoreId)
    {
        var storeId = await productScope.GetDefaultStoreIdAsync();
        return storeId == null || (unitStoreId ?? "") == storeId;
    }

    private IActionResult Denied(string list)
    {
        Error(translationService.GetResource("Admin.Inventory.Documents.AccessDenied"));
        return RedirectToAction(list);
    }

    private DateTime Today()
    {
        return dateTimeService.ConvertToUserTime(DateTime.UtcNow, DateTimeKind.Utc).Date;
    }

    private async Task AddWarehouses(IList<SelectListItem> list)
    {
        list.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var warehouse in await inventoryDocumentViewModelService.UsableWarehouses())
            list.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id });
    }

    private async Task<string> WarehouseName(string warehouseId)
    {
        return string.IsNullOrEmpty(warehouseId)
            ? ""
            : (await warehouseService.GetWarehouseById(warehouseId))?.Name ?? warehouseId;
    }

    private async Task<T> Row<T>(ProductSerialNumber serialNumber, T row) where T : SerialNumberRowModel
    {
        row.Id = serialNumber.Id;
        row.Number = serialNumber.Number;
        row.ProductId = serialNumber.ProductId;
        row.ProductName = (await productService.GetProductById(serialNumber.ProductId))?.Name;
        row.Status = enumTranslationService.GetTranslationEnum(serialNumber.StatusId);
        row.Warehouse = await WarehouseName(serialNumber.WarehouseId);
        row.OrderId = serialNumber.OrderId;
        row.ShippedOn = serialNumber.ShippedOnUtc.HasValue
            ? dateTimeService.ConvertToUserTime(serialNumber.ShippedOnUtc.Value, DateTimeKind.Utc)
            : null;
        row.WarrantyUntil = serialNumber.WarrantyUntilUtc.HasValue
            ? dateTimeService.ConvertToUserTime(serialNumber.WarrantyUntilUtc.Value, DateTimeKind.Utc)
            : null;
        return row;
    }

    private async Task<T> Row<T>(ProductBatch batch, T row) where T : BatchRowModel
    {
        var product = await productService.GetProductById(batch.ProductId);
        row.Id = batch.Id;
        row.Number = batch.Number;
        row.ProductId = batch.ProductId;
        row.ProductName = product?.Name;
        row.Warehouse = await WarehouseName(batch.WarehouseId);
        row.ExpiryDate = batch.ExpiryDateUtc;
        row.Quantity = batch.Quantity;
        var status = product == null
            ? null
            : BatchExpiry.Status(batch.ExpiryDateUtc, Today(), product.ExpiryWarningDays ?? 30);
        row.ExpiryStatus = status.HasValue ? enumTranslationService.GetTranslationEnum(status.Value) : "";
        return row;
    }

    private async Task<IList<StockHistoryRowModel>> History(string productId, string batchNumber,
        string serialNumber)
    {
        var rows = new List<StockHistoryRowModel>();
        foreach (var entry in await productUnitService.History(productId, batchNumber, serialNumber))
            rows.Add(new StockHistoryRowModel {
                Date = dateTimeService.ConvertToUserTime(entry.CreateDateUtc, DateTimeKind.Utc),
                MovementType = enumTranslationService.GetTranslationEnum(entry.MovementTypeId),
                Warehouse = await WarehouseName(entry.WarehouseId),
                In = entry.InQty,
                Out = entry.OutQty,
                Reference = entry.Reference,
                DocumentId = entry.ObjectType == nameof(InventoryDocument) ? entry.ObjectId : null,
                ShipmentId = entry.ObjectType == nameof(Shipment) ? entry.ObjectId : null,
                Comment = entry.Comments
            });
        return rows;
    }
}
