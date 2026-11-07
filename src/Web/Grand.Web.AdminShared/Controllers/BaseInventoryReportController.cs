using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
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

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     The stock a warehouse held at a date, worked out from the journal. Store reports on its own warehouses only
/// </summary>
[PermissionAuthorize(PermissionSystemName.InventoryDocuments)]
[AutoValidateAntiforgeryToken]
public abstract class BaseInventoryReportController(
    IInventoryJournalService inventoryJournalService,
    IInventoryDocumentViewModelService inventoryDocumentViewModelService,
    IWarehouseService warehouseService,
    IProductService productService,
    IDateTimeService dateTimeService,
    IRequestTranslationService translationService,
    IAdminDataScope<Warehouse> warehouseScope,
    IAdminDataScope<Product> productScope) : BaseController
{
    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpGet]
    public async Task<IActionResult> StockAtDate()
    {
        var model = new StockAtDateModel { AtDate = dateTimeService.ConvertToUserTime(DateTime.UtcNow, DateTimeKind.Utc) };
        foreach (var warehouse in await inventoryDocumentViewModelService.UsableWarehouses())
            model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id });
        return View("StockAtDate", model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> StockAtDateData(DataSourceRequest command, string warehouseId, DateTime? atDate)
    {
        var warehouse = string.IsNullOrEmpty(warehouseId) ? null : await warehouseService.GetWarehouseById(warehouseId);
        if (warehouse == null || !await warehouseScope.HasAccess(warehouse))
            return ErrorForKendoGridJson(translationService.GetResource("Admin.Inventory.Documents.AccessDenied"));

        var atUtc = atDate.HasValue
            ? dateTimeService.ConvertToUtcTime(atDate.Value, dateTimeService.CurrentTimeZone)
            : DateTime.UtcNow;
        //a warehouse of this store may hold a product of another one
        var stock = await inventoryJournalService.GetStockAtDate(warehouse.Id, atUtc,
            await productScope.GetDefaultStoreIdAsync());

        var rows = new List<StockAtDateRowModel>();
        foreach (var row in stock)
        {
            var product = await productService.GetProductById(row.ProductId);
            if (product == null)
                continue;

            rows.Add(new StockAtDateRowModel {
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                Sku = row.Sku,
                Combination = string.Join(", ",
                    product.ParseProductAttributeValues(row.Attributes).Select(x => x.Name)),
                Stock = row.Available ? row.Stock.ToString() : "-"
            });
        }

        return Json(new DataSourceResult { Data = rows, Total = rows.Count });
    }
}
