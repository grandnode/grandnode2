using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.MerchandiseReturns)]
[AutoValidateAntiforgeryToken]
public abstract class BaseMerchandiseReturnController(
    IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
    IRequestTranslationService translationService,
    IMerchandiseReturnService merchandiseReturnService,
    IOrderService orderService,
    IAdminDataScope<MerchandiseReturn> scope)
    : BaseController
{
    // Exposed for host subclasses: primary-constructor parameters aren't visible to derived classes
    // by name in C#.
    protected IMerchandiseReturnViewModelService MerchandiseReturnViewModelService => merchandiseReturnViewModelService;
    protected IRequestTranslationService TranslationService => translationService;
    protected IMerchandiseReturnService MerchandiseReturnService => merchandiseReturnService;
    protected IOrderService OrderService => orderService;
    protected IAdminDataScope<MerchandiseReturn> Scope => scope;

    /// <summary>
    ///     Whether the panel has inventory documents: the return's receipts are listed and "Receive to stock" is
    ///     offered while something is left to receive
    /// </summary>
    protected virtual bool HasInventoryDocuments => true;

    /// <summary>
    ///     Whether the panel gives back the money for a received return through the payment of its order; a panel
    ///     that does not shows the refund once made
    /// </summary>
    protected virtual bool RefundsPayments => true;

    private async Task PrepareEditModel(MerchandiseReturnModel model, MerchandiseReturn merchandiseReturn)
    {
        await MerchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, merchandiseReturn, false);
        if (HasInventoryDocuments)
            await MerchandiseReturnViewModelService.PrepareReturnReceipts(model);
        await MerchandiseReturnViewModelService.PrepareRefund(model, merchandiseReturn, RefundsPayments);
    }

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        var model = merchandiseReturnViewModelService.PrepareReturnRequestListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, MerchandiseReturnListModel model)
    {
        // Vendor needs no analogous model.StoreId forcing here (no store concept) - its
        // vendor-scoping happens inside the shared service call via scope.DefaultVendorId (Task 4).
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;

        var (merchandiseReturnModels, totalCount) =
            await merchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult {
            Data = merchandiseReturnModels,
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion

    #region GoToId / Products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToId(MerchandiseReturnListModel model)
    {
        if (model.GoDirectlyToId == null)
            return RedirectToAction("List");

        int.TryParse(model.GoDirectlyToId, out var id);

        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null || !await Scope.HasAccess(merchandiseReturn))
            return RedirectToAction("List");

        return RedirectToAction("Edit", new { id = merchandiseReturn.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductsForMerchandiseReturn(string merchandiseReturnId, DataSourceRequest command)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || !await Scope.HasAccess(merchandiseReturn))
            return ErrorForKendoGridJson("Merchandise return not found");

        var items = await MerchandiseReturnViewModelService.PrepareMerchandiseReturnItemModel(merchandiseReturnId);
        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };
        return Json(gridModel);
    }

    #endregion

    #region Edit

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!await Scope.CanView(merchandiseReturn)) return RedirectToAction("List");

        var model = new MerchandiseReturnModel();
        await PrepareEditModel(model, merchandiseReturn);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(MerchandiseReturnModel model, bool continueEditing,
        [FromServices] Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService addressAttributeService,
        [FromServices] Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser addressAttributeParser,
        [FromServices] Grand.Domain.Orders.OrderSettings orderSettings)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(model.Id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!await Scope.HasAccess(merchandiseReturn)) return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            var customAddressAttributes = new List<Grand.Domain.Common.CustomAttribute>();
            if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
                customAddressAttributes = await model.PickupAddress.ParseCustomAddressAttributes(
                    addressAttributeParser, addressAttributeService);

            var (updated, error) = await MerchandiseReturnViewModelService.UpdateMerchandiseReturnModel(
                merchandiseReturn, model, customAddressAttributes);

            //a status changed comes back to the return; a refused one shows the form again, with the reason typed
            if (model.ChangeStatusTo.HasValue && error == null)
            {
                Success(TranslationService.GetResource(
                    $"{Scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.StatusChanged"));
                return RedirectToAction("Edit", new { id = merchandiseReturn.Id });
            }

            if (error == null)
            {
                Success(TranslationService.GetResource(
                    $"{Scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.Updated"));
                return continueEditing
                    ? RedirectToAction("Edit", new { id = updated.Id })
                    : RedirectToAction("List");
            }

            Error(TranslationService.GetResource($"{Scope.ResourceKeyPrefix}.{error}"));
            //the status buttons follow the return as stored now: another change may be why this one was refused
            merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(model.Id) ?? merchandiseReturn;
        }

        await PrepareEditModel(model, merchandiseReturn);
        return View(model);
    }

    #endregion

    #region Return method

    /// <summary>
    ///     The label of the return, to print and stick on the parcel
    /// </summary>
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Label(string id, [FromServices] IDownloadService downloadService)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!await Scope.CanView(merchandiseReturn)) return RedirectToAction("List");

        var shipping = merchandiseReturn.ReturnShipping;
        if (shipping?.LabelStatus != ReturnLabelStatus.Created || string.IsNullOrEmpty(shipping.LabelDownloadId))
            return RedirectToAction("Edit", new { id = merchandiseReturn.Id });

        var download = await downloadService.GetDownloadById(shipping.LabelDownloadId);
        if (download?.DownloadBinary == null)
            return RedirectToAction("Edit", new { id = merchandiseReturn.Id });

        return File(download.DownloadBinary,
            string.IsNullOrEmpty(download.ContentType) ? "application/octet-stream" : download.ContentType,
            download.Filename + download.Extension);
    }

    /// <summary>
    ///     Asks the provider again for a label it has not created; the label service takes the label's lock, so a
    ///     double click or the customer's retry at the same time buys one label only
    /// </summary>
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RetryLabel(string id,
        [FromServices] IMerchandiseReturnLabelService merchandiseReturnLabelService)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!await Scope.HasAccess(merchandiseReturn)) return RedirectToAction("List");

        var error = await merchandiseReturnLabelService.CreateLabel(merchandiseReturn.Id);
        if (error != null)
            Error(TranslationService.GetResource($"{Scope.ResourceKeyPrefix}.{error}"));

        //a label the provider failed again is shown on the return, with the provider's error
        return RedirectToAction("Edit", new { id = merchandiseReturn.Id });
    }

    #endregion

    #region Refund

    /// <summary>
    ///     Gives back the money for the received return, through the payment provider or offline, and completes it.
    ///     Posts the edit form to its own action: what was typed and not saved yet is not saved by it
    /// </summary>
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> Refund(string id, double refundAmount, bool offline)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!RefundsPayments || !await Scope.HasAccess(merchandiseReturn)) return RedirectToAction("List");

        var error = await MerchandiseReturnViewModelService.RefundMerchandiseReturn(merchandiseReturn, refundAmount,
            offline);
        if (error == null)
            Success(TranslationService.GetResource(
                $"{Scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.Refund.Refunded"));
        else
            Error(TranslationService.GetResource($"{Scope.ResourceKeyPrefix}.{error}"));

        return RedirectToAction("Edit", new { id = merchandiseReturn.Id });
    }

    /// <summary>
    ///     Records whether the money of a refund that waits for its check went back: the return is refunded and
    ///     completed, or it can be refunded again. Posts the edit form to its own action, as the refund does
    /// </summary>
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ResolveRefund(string id, bool wentBack)
    {
        var merchandiseReturn = await MerchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!RefundsPayments || !await Scope.HasAccess(merchandiseReturn)) return RedirectToAction("List");

        var error = await MerchandiseReturnViewModelService.ResolveRefund(merchandiseReturn, wentBack);
        if (error == null)
            Success(TranslationService.GetResource(wentBack
                ? $"{Scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.Refund.Refunded"
                : $"{Scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.Refund.Cleared"));
        else
            Error(TranslationService.GetResource($"{Scope.ResourceKeyPrefix}.{error}"));

        return RedirectToAction("Edit", new { id = merchandiseReturn.Id });
    }

    #endregion

    #region Delete

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(id);
        if (merchandiseReturn == null) return RedirectToAction("List");
        if (!await scope.HasAccess(merchandiseReturn)) return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            var error = await merchandiseReturnViewModelService.DeleteMerchandiseReturn(merchandiseReturn);
            if (error == null)
                Success(translationService.GetResource(
                    $"{scope.ResourceKeyPrefix}.Orders.MerchandiseReturns.Deleted"));
            else
                Error(translationService.GetResource($"{scope.ResourceKeyPrefix}.{error}"));
        }

        return RedirectToAction("List");
    }

    #endregion

    #region Merchandise return notes

    /// <summary>Deliberately preserved host divergence (spec §5/§11, DECIDED — not unified): Admin's
    /// and Vendor's original controllers both throw ArgumentException for a not-found-or-denied
    /// merchandise return; Store's soft-denies with an empty Content("") response instead, because
    /// Store's Kendo grid error path expects a 200 with an empty body, not a 500. This seam is
    /// written from scratch for this phase - it is NOT copied from BaseOrderManagementController's
    /// OrderNotesSelect, which (as shipped) has no host-differentiated response at all (see this
    /// plan's Global Constraints for the full explanation). Default here matches Admin/Vendor; only
    /// Grand.Web.Store's concrete MerchandiseReturnController overrides this (Task 10).</summary>
    protected virtual IActionResult NotFoundOrDeniedForNotesSelect() =>
        throw new ArgumentException("No merchandise return found with the specified id");

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnNotesSelect(string merchandiseReturnId, DataSourceRequest command)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || !await scope.HasAccess(merchandiseReturn))
            return NotFoundOrDeniedForNotesSelect();

        var merchandiseReturnNoteModels = await merchandiseReturnViewModelService.PrepareMerchandiseReturnNotes(merchandiseReturn);
        var gridModel = new DataSourceResult {
            Data = merchandiseReturnNoteModels,
            Total = merchandiseReturnNoteModels.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnNoteAdd(string merchandiseReturnId, string downloadId,
        bool displayToCustomer, string message)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null || !await scope.HasAccess(merchandiseReturn))
            return Json(new { Result = false });

        // Spec §2.5/§11 (approved): no caller-supplied orderId - always resolve the entity's own
        // order server-side. Closes Admin's original gap (never validated a caller-supplied orderId
        // matched the merchandise return), makes Store's now-redundant equality check unnecessary,
        // and needs no new parameter on Vendor's side.
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        if (order == null) return Json(new { Result = false });

        await merchandiseReturnViewModelService.InsertMerchandiseReturnNote(merchandiseReturn, order, downloadId,
            displayToCustomer, message);

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MerchandiseReturnNoteDelete(string id, string merchandiseReturnId)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null)
            throw new ArgumentException("No merchandise return found with the specified id");
        if (!await scope.HasAccess(merchandiseReturn))
            return Json(new { Result = false });

        await merchandiseReturnViewModelService.DeleteMerchandiseReturnNote(merchandiseReturn, id);

        return new JsonResult("");
    }

    #endregion
}
