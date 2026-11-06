using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Inventory;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     Inventory documents of Main Admin and the Store panel. Store works within its own warehouses: a document of
///     another store, or one naming another store's or a shared warehouse, is neither shown nor saved, and every
///     product added passes the panel's product access check
/// </summary>
public abstract class BaseInventoryDocumentController(
    IInventoryDocumentService inventoryDocumentService,
    IInventoryDocumentViewModelService inventoryDocumentViewModelService,
    IWarehouseService warehouseService,
    IMerchandiseReturnService merchandiseReturnService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    IAdminDataScope<Warehouse> warehouseScope,
    IAdminDataScope<MerchandiseReturn> merchandiseReturnScope) : BaseController
{
    private string CustomerId => contextAccessor.WorkContext.CurrentCustomer.Id;

    /// <summary>
    ///     Whether the warehouse may be put on a document; none chosen yet is allowed on a draft
    /// </summary>
    private async Task<bool> CanUse(string warehouseId)
    {
        if (string.IsNullOrEmpty(warehouseId))
            return true;

        var warehouse = await warehouseService.GetWarehouseById(warehouseId);
        return warehouse != null && await warehouseScope.HasAccess(warehouse);
    }

    private async Task<bool> HasAccess(InventoryDocument document)
    {
        var storeId = await warehouseScope.GetDefaultStoreIdAsync();
        return document != null && (storeId == null || document.StoreId == storeId) &&
               await CanUse(document.WarehouseId) && await CanUse(document.TargetWarehouseId);
    }

    private IActionResult Denied()
    {
        Error(translationService.GetResource("Admin.Inventory.Documents.AccessDenied"));
        return RedirectToAction("List");
    }

    protected virtual async Task<IActionResult> ListCore()
    {
        return View("List", await inventoryDocumentViewModelService.PrepareListModel());
    }

    protected virtual async Task<IActionResult> ListDataCore(DataSourceRequest command,
        InventoryDocumentListModel model)
    {
        var (rows, totalCount) =
            await inventoryDocumentViewModelService.PrepareRows(model, command.Page - 1, command.PageSize);
        return Json(new DataSourceResult { Data = rows, Total = totalCount });
    }

    protected virtual async Task<IActionResult> CreateGetCore(InventoryDocumentType type)
    {
        var model = new InventoryDocumentModel { DocumentTypeId = (int)type };
        await inventoryDocumentViewModelService.PrepareModel(model, null);
        return View("Create", model);
    }

    protected virtual async Task<IActionResult> CreatePostCore(InventoryDocumentModel model)
    {
        if (string.IsNullOrEmpty(model.WarehouseId) || !await CanUse(model.WarehouseId) ||
            !await CanUse(model.TargetWarehouseId))
            return Denied();

        var type = (InventoryDocumentType)model.DocumentTypeId;
        if (type == InventoryDocumentType.StockCount)
        {
            var count = await inventoryDocumentService.CreateStockCount(model.WarehouseId, model.CategoryIds,
                model.BrandIds, CustomerId);
            return RedirectToAction("Edit", new { id = count.Id });
        }

        var document = new InventoryDocument { DocumentTypeId = type, CreatedBy = CustomerId };
        var errors = await inventoryDocumentViewModelService.ApplyModel(document, model);
        if (errors.Count > 0)
        {
            Error(string.Join(" ", errors));
            await inventoryDocumentViewModelService.PrepareModel(model, null);
            return View("Create", model);
        }

        document = await inventoryDocumentService.CreateDraft(document);
        return RedirectToAction("Edit", new { id = document.Id });
    }

    protected virtual async Task<IActionResult> CreateReturnReceiptCore(string merchandiseReturnId)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || !await merchandiseReturnScope.HasAccess(merchandiseReturn))
            return Denied();

        //the warehouse the items were sold from may be one this panel does not book: one of its own is taken
        var usable = await inventoryDocumentViewModelService.UsableWarehouses();
        if (usable.Count == 0)
            return Denied();

        var (document, errors) =
            await inventoryDocumentService.CreateReturnReceipt(merchandiseReturnId, CustomerId);
        if (document == null)
        {
            Error(string.Join(" ", inventoryDocumentViewModelService.Translate(errors)));
            return RedirectToAction("List");
        }

        if (string.IsNullOrEmpty(document.WarehouseId) || !await CanUse(document.WarehouseId))
        {
            document.WarehouseId = usable[0].Id;
            await inventoryDocumentService.UpdateDraft(document);
        }

        return RedirectToAction("Edit", new { id = document.Id });
    }

    protected virtual async Task<IActionResult> EditGetCore(string id)
    {
        var document = await inventoryDocumentService.GetById(id);
        if (!await HasAccess(document))
            return Denied();

        var model = new InventoryDocumentModel();
        await inventoryDocumentViewModelService.PrepareModel(model, document);
        return View("Edit", model);
    }

    protected virtual async Task<IActionResult> EditPostCore(InventoryDocumentModel model, bool continueEditing)
    {
        var document = await inventoryDocumentService.GetById(model.Id);
        if (!await HasAccess(document) || string.IsNullOrEmpty(model.WarehouseId) ||
            !await CanUse(model.WarehouseId) || !await CanUse(model.TargetWarehouseId))
            return Denied();

        if (document.StatusId == InventoryDocumentStatus.Posted)
        {
            Error(translationService.GetResource(InventoryDocumentErrors.Posted));
            return RedirectToAction("Edit", new { id = document.Id });
        }

        var errors = await inventoryDocumentViewModelService.ApplyModel(document, model);
        if (errors.Count == 0)
            errors = inventoryDocumentViewModelService.Translate(await inventoryDocumentService.UpdateDraft(document));

        if (errors.Count > 0)
            Error(string.Join(" ", errors));
        else
            Success(translationService.GetResource("Admin.Inventory.Documents.Updated"));

        return continueEditing || errors.Count > 0
            ? RedirectToAction("Edit", new { id = document.Id })
            : RedirectToAction("List");
    }

    protected virtual async Task<IActionResult> PostCore(string id)
    {
        var document = await inventoryDocumentService.GetById(id);
        if (!await HasAccess(document))
            return Denied();

        var errors = await inventoryDocumentService.Post(document.Id, CustomerId);
        if (errors.Count > 0)
            Error(string.Join(" ", inventoryDocumentViewModelService.Translate(errors)));
        else
            Success(translationService.GetResource("Admin.Inventory.Documents.PostedSuccessfully"));

        return RedirectToAction("Edit", new { id = document.Id });
    }

    protected virtual async Task<IActionResult> DeleteCore(string id)
    {
        var document = await inventoryDocumentService.GetById(id);
        if (!await HasAccess(document))
            return Denied();

        var errors = await inventoryDocumentService.DeleteDraft(document.Id);
        if (errors.Count > 0)
        {
            Error(string.Join(" ", inventoryDocumentViewModelService.Translate(errors)));
            return RedirectToAction("Edit", new { id = document.Id });
        }

        Success(translationService.GetResource("Admin.Inventory.Documents.Deleted"));
        return RedirectToAction("List");
    }
}
