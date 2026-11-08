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
using Microsoft.AspNetCore.Http;
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
    IAdminDataScope<MerchandiseReturn> merchandiseReturnScope,
    IProductAddPopupViewModelService productAddPopupViewModelService) : BaseController
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
               await CanUse(document.WarehouseId) && await CanUse(document.TargetWarehouseId) &&
               (await inventoryDocumentViewModelService.InaccessibleLines(document)).Count == 0;
    }

    /// <summary>
    ///     A draft built from a warehouse or a merchandise return may name products of other stores: their lines are
    ///     dropped, and a draft left with none is deleted
    /// </summary>
    /// <returns>False when the draft was deleted</returns>
    private async Task<bool> KeepAccessibleLines(InventoryDocument document, bool changed)
    {
        foreach (var line in await inventoryDocumentViewModelService.InaccessibleLines(document))
        {
            document.Lines.Remove(line);
            changed = true;
        }

        if (document.Lines.Count == 0)
        {
            await inventoryDocumentService.DeleteDraft(document.Id);
            Error(translationService.GetResource(InventoryDocumentErrors.NoLines));
            return false;
        }

        if (changed)
            await inventoryDocumentService.UpdateDraft(document);
        return true;
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
        if (string.IsNullOrEmpty(model.WarehouseId))
        {
            Error(translationService.GetResource(InventoryDocumentErrors.WarehouseNotFound));
            await inventoryDocumentViewModelService.PrepareModel(model, null);
            return View("Create", model);
        }

        if (!await CanUse(model.WarehouseId) || !await CanUse(model.TargetWarehouseId))
            return Denied();

        var type = (InventoryDocumentType)model.DocumentTypeId;
        if (type == InventoryDocumentType.StockCount)
        {
            var (count, refused) = await inventoryDocumentService.CreateStockCount(model.WarehouseId,
                model.CategoryIds, model.BrandIds, CustomerId);
            if (count == null)
            {
                Error(string.Join(" ", inventoryDocumentViewModelService.Translate(refused)));
                await inventoryDocumentViewModelService.PrepareModel(model, null);
                return View("Create", model);
            }

            return await KeepAccessibleLines(count, false)
                ? RedirectToAction("Edit", new { id = count.Id })
                : RedirectToAction("List");
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

        var otherWarehouse = string.IsNullOrEmpty(document.WarehouseId) || !await CanUse(document.WarehouseId);
        if (otherWarehouse)
            document.WarehouseId = usable[0].Id;

        return await KeepAccessibleLines(document, otherWarehouse)
            ? RedirectToAction("Edit", new { id = document.Id })
            : RedirectToAction("List");
    }

    protected virtual async Task<IActionResult> EditGetCore(string id, int page)
    {
        var document = await inventoryDocumentService.GetById(id);
        if (!await HasAccess(document))
            return Denied();

        var model = new InventoryDocumentModel();
        await inventoryDocumentViewModelService.PrepareModel(model, document, page);
        return View("Edit", model);
    }

    protected virtual async Task<IActionResult> EditPostCore(InventoryDocumentModel model, bool continueEditing)
    {
        var document = await inventoryDocumentService.GetById(model.Id);
        if (!await HasAccess(document))
            return Denied();

        if (string.IsNullOrEmpty(model.WarehouseId))
        {
            Error(translationService.GetResource(InventoryDocumentErrors.WarehouseNotFound));
            return RedirectToAction("Edit", new { id = document.Id });
        }

        if (!await CanUse(model.WarehouseId) || !await CanUse(model.TargetWarehouseId))
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

        //the form holds a page of lines: the pager saves it and turns the page
        var page = model.LinesTarget ?? model.LinesPage;
        return continueEditing || errors.Count > 0 || model.LinesTarget.HasValue
            ? RedirectToAction("Edit", new { id = document.Id, page })
            : RedirectToAction("List");
    }

    protected virtual async Task<IActionResult> SearchProductsCore(string id, DataSourceRequestFilter model)
    {
        var document = await inventoryDocumentService.GetById(id);
        if (!await HasAccess(document))
            return Json(new DataSourceResult { Data = Array.Empty<InventoryDocumentProductModel>() });

        return Json(new DataSourceResult {
            Data = await inventoryDocumentViewModelService.SearchProducts(document, model.GetNameFilterValue())
        });
    }

    protected virtual async Task<IActionResult> ProductCombinationsCore(string id, string productId)
    {
        var document = await inventoryDocumentService.GetById(id);
        if (!await HasAccess(document))
            return Json(Array.Empty<InventoryDocumentCombinationModel>());

        return Json(await inventoryDocumentViewModelService.ProductCombinations(document, productId));
    }

    /// <summary>
    ///     Adds the product the picker chose without leaving the page: the form is saved with it, so what was typed in
    ///     the other lines stays, and the lines are drawn again on the page holding the line added to
    /// </summary>
    protected virtual async Task<IActionResult> AddLinesCore(InventoryDocumentModel model)
    {
        var document = await inventoryDocumentService.GetById(model.Id);
        if (!await HasAccess(document) || !await CanUse(model.WarehouseId) || !await CanUse(model.TargetWarehouseId))
            return new ContentResult {
                StatusCode = StatusCodes.Status403Forbidden,
                Content = translationService.GetResource("Admin.Inventory.Documents.AccessDenied")
            };

        IList<string> errors = [];
        IList<string> notices = [];
        string lineId = null;
        if (document.StatusId == InventoryDocumentStatus.Posted)
            errors = [translationService.GetResource(InventoryDocumentErrors.Posted)];
        if (errors.Count == 0)
            errors = await inventoryDocumentViewModelService.ApplyModel(document, model);
        if (errors.Count == 0 && model.AddProductIds.Count > 0)
        {
            //the products of the bulk popup that cannot be booked are named, the others added
            (var leftOut, lineId) =
                await inventoryDocumentViewModelService.AddProducts(document, model.AddProductIds);
            if (leftOut.Count > 0)
                notices = [$"{translationService.GetResource("Admin.Inventory.Documents.ProductsLeftOut")} {string.Join(", ", leftOut)}"];
        }
        else if (errors.Count == 0)
            (errors, lineId) = await inventoryDocumentViewModelService.AddLines(document, model);
        if (errors.Count == 0)
            errors = inventoryDocumentViewModelService.Translate(await inventoryDocumentService.UpdateDraft(document));

        //a draft that could not be saved is drawn as it is stored
        if (errors.Count > 0)
        {
            document = await inventoryDocumentService.GetById(document.Id);
            lineId = null;
        }

        var added = document.Lines.FirstOrDefault(x => x.Id == lineId);
        var page = added == null
            ? model.LinesPage
            : document.Lines.IndexOf(added) / InventoryDocumentViewModelService.LinesPageSize + 1;
        var lines = new InventoryDocumentModel();
        await inventoryDocumentViewModelService.PrepareModel(lines, document, page);

        //the posted values of the lines must not be drawn over the lines as they are now
        ModelState.Clear();
        ViewData["AddedLineId"] = lineId;
        ViewData["AddErrors"] = errors;
        ViewData["AddNotices"] = errors.Count == 0 ? notices : [];
        return PartialView("Partials/Edit.Lines", lines);
    }

    /// <summary>
    ///     The bulk popup: the search the other product popups have, with products checked to add
    /// </summary>
    protected virtual async Task<IActionResult> ProductAddPopupCore(string id)
    {
        var document = await inventoryDocumentService.GetById(id);
        if (!await HasAccess(document))
            return Content(translationService.GetResource("Admin.Inventory.Documents.AccessDenied"));

        var model = await productAddPopupViewModelService.PrepareAddProductModel<InventoryDocumentProductAddModel>();
        model.DocumentId = document.Id;
        return View("ProductAddPopup", model);
    }

    protected virtual async Task<IActionResult> ProductAddPopupListCore(DataSourceRequest command,
        InventoryDocumentProductAddModel model)
    {
        //Store lists the products of its own store
        if (warehouseScope.DefaultStoreId is not null) model.SearchStoreId = warehouseScope.DefaultStoreId;

        var (products, totalCount) =
            await productAddPopupViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        return Json(new DataSourceResult { Data = products, Total = totalCount });
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
