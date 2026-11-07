using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Inventory;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     The stock history tab of the product: its journal entries, newest first. Store sees the entries of its own
///     warehouses and of shared ones
/// </summary>
[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductStockHistoryController(
    IProductService productService,
    IInventoryJournalService inventoryJournalService,
    IWarehouseService warehouseService,
    ICustomerService customerService,
    IEnumTranslationService enumTranslationService,
    IDateTimeService dateTimeService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope) : BaseController
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ListStockHistory(DataSourceRequest command, string productId,
        string warehouseId, DateTime? from, DateTime? to)
    {
        var product = await productService.GetProductById(productId);
        if (product == null || !await scope.CanView(product))
            return ErrorForKendoGridJson(
                translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        //a product shared with other stores opens read-only; its history carries their movements and staff, so it
        //is listed only for a product this store owns, as its bids and reservations are
        if (!await scope.HasAccess(product))
            return Json(new DataSourceResult { Data = Array.Empty<object>(), Total = 0 });

        var storeId = await scope.GetDefaultStoreIdAsync();
        var entries = await inventoryJournalService.GetProductHistory(productId, warehouseId,
            from.HasValue ? dateTimeService.ConvertToUtcTime(from.Value, dateTimeService.CurrentTimeZone) : null,
            to.HasValue ? dateTimeService.ConvertToUtcTime(to.Value, dateTimeService.CurrentTimeZone) : null,
            storeId == null ? null : [storeId, ""], command.Page - 1, command.PageSize);

        var warehouses = (await warehouseService.GetAllWarehouses()).ToDictionary(x => x.Id, x => x.Name);
        var customers = new Dictionary<string, string>();
        foreach (var customerId in entries.Select(x => x.CustomerId).Where(x => !string.IsNullOrEmpty(x)).Distinct())
            customers[customerId] = (await customerService.GetCustomerById(customerId))?.Email ?? "";

        var rows = entries.Select(x => new StockHistoryRowModel {
            Date = dateTimeService.ConvertToUserTime(x.CreateDateUtc, DateTimeKind.Utc),
            MovementType = enumTranslationService.GetTranslationEnum(x.MovementTypeId),
            Warehouse = warehouses.GetValueOrDefault(x.WarehouseId ?? "") ?? "",
            Combination = string.Join(", ",
                product.ParseProductAttributeValues(x.Attributes ?? []).Select(v => v.Name)),
            In = x.InQty,
            Out = x.OutQty,
            Reference = x.Reference,
            DocumentId = x.ObjectType == nameof(InventoryDocument) ? x.ObjectId : null,
            ShipmentId = x.ObjectType == nameof(Shipment) ? x.ObjectId : null,
            Who = customers.GetValueOrDefault(x.CustomerId ?? "") ?? "",
            Comment = x.Comments
        }).ToList();
        return Json(new DataSourceResult { Data = rows, Total = entries.TotalCount });
    }
}
