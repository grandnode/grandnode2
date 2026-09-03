using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.GiftVouchers)]
[AutoValidateAntiforgeryToken]
public abstract class BaseGiftVoucherController(
    IGiftVoucherViewModelService giftVoucherViewModelService,
    IGiftVoucherService giftVoucherService,
    ITranslationService translationService,
    IAdminDataScope<GiftVoucher> scope)
    : BaseController
{
    /// <summary>Hook for host-specific UI-copy warnings that aren't access-scope decisions.
    /// Overridden by the Store subclass (Task 5); no-op everywhere else. Mirrors
    /// BaseCategoryController.EditWarningCheck.</summary>
    protected virtual void EditWarningCheck(GiftVoucher giftVoucher) { }

    // Exposed for host subclasses: primary-constructor parameters are not visible to derived
    // classes by name in C#, so Store's EditWarningCheck override needs this.
    protected ITranslationService TranslationService => translationService;
    protected IAdminDataScope<GiftVoucher> Scope => scope;

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        var model = giftVoucherViewModelService.PrepareGiftVoucherListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> GiftVoucherList(DataSourceRequest command, GiftVoucherListModel model)
    {
        var (giftVoucherModels, totalCount) = await giftVoucherViewModelService.PrepareGiftVoucherModel(
            model, command.Page, command.PageSize, scope.DefaultStoreId ?? "");

        return Json(new DataSourceResult {
            Data = giftVoucherModels.ToList(),
            Total = totalCount
        });
    }

    #endregion

    #region Create

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await giftVoucherViewModelService.PrepareGiftVoucherModel();
        return ApplyDefaultStore(model);
    }

    // Was PermissionActionName.Edit on Admin's pre-consolidation controller while Admin's own GET
    // and both of Store's Create actions required Create - a disclosed bug fix, not a new
    // restriction. See spec "Design > BaseGiftVoucherController" bullet on the Create(POST) fix.
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(GiftVoucherModel model, bool continueEditing)
    {
        if (!string.IsNullOrEmpty(scope.DefaultStoreId)) model.StoreId = scope.DefaultStoreId;

        if (ModelState.IsValid)
        {
            var giftVoucher = await giftVoucherViewModelService.InsertGiftVoucherModel(model);
            Success(translationService.GetResource("Admin.GiftVouchers.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = giftVoucher.Id }) : RedirectToAction("List");
        }

        model = await giftVoucherViewModelService.PrepareGiftVoucherModel(model);
        return View(ApplyDefaultStoreToModel(model));
    }

    #endregion

    #region Edit / Delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var giftVoucher = await giftVoucherService.GetGiftVoucherById(id);
        if (giftVoucher == null) return RedirectToAction("List");

        EditWarningCheck(giftVoucher);
        // CanView, not HasAccess: viewing a global (empty-StoreId) voucher is allowed on Store
        // (with a warning from EditWarningCheck above); only mutating one is restricted to the
        // exclusive single-store owner. See IAdminDataScope<TEntity>.CanView's doc comment.
        if (!await scope.CanView(giftVoucher)) return RedirectToAction("List");

        var model = await giftVoucherViewModelService.PrepareGiftVoucherModel(giftVoucher);
        return View(ApplyDefaultStoreToModel(model));
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(GiftVoucherModel model, bool continueEditing)
    {
        var giftVoucher = await giftVoucherService.GetGiftVoucherById(model.Id);
        if (giftVoucher == null) return RedirectToAction("List");
        if (!await scope.HasAccess(giftVoucher)) return RedirectToAction("Edit", new { id = giftVoucher.Id });

        if (!string.IsNullOrEmpty(scope.DefaultStoreId)) model.StoreId = scope.DefaultStoreId;
        await giftVoucherViewModelService.FillGiftVoucherModel(giftVoucher, model);

        if (ModelState.IsValid)
        {
            giftVoucher = await giftVoucherViewModelService.UpdateGiftVoucherModel(giftVoucher, model);
            Success(translationService.GetResource("Admin.GiftVouchers.Updated"));

            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = giftVoucher.Id });
            }

            return RedirectToAction("List");
        }

        return View(ApplyDefaultStoreToModel(model));
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> NotifyRecipient(GiftVoucherNotifyRecipient model)
    {
        var giftVoucher = await giftVoucherService.GetGiftVoucherById(model.Id);
        if (giftVoucher == null) return RedirectToAction("List");
        if (!await scope.HasAccess(giftVoucher)) return RedirectToAction("Edit", new { id = model.Id });

        try
        {
            if (ModelState.IsValid)
                await giftVoucherViewModelService.NotifyRecipient(giftVoucher);
            else
                Error(ModelState);
        }
        catch (Exception exc)
        {
            Error(exc, false);
        }

        return RedirectToAction("Edit", new { id = model.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(GiftVoucherDeleteModel model)
    {
        var giftVoucher = await giftVoucherService.GetGiftVoucherById(model.Id);
        if (giftVoucher == null) return RedirectToAction("List");
        if (!await scope.HasAccess(giftVoucher)) return RedirectToAction("Edit", new { id = giftVoucher.Id });

        if (ModelState.IsValid)
        {
            await giftVoucherViewModelService.DeleteGiftVoucher(giftVoucher);
            Success(translationService.GetResource("Admin.GiftVouchers.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = giftVoucher.Id });
    }

    #endregion

    #region Gift voucher usage history

    // CanView, not HasAccess: this is a read-only grid on the Edit screen's History tab, and Edit
    // is reachable (read-only) for a global voucher via CanView above - History must follow or
    // the tab breaks. Same "sibling read action must match its screen's view-gate" fix as
    // ARCH-001 News's Comments-tab Critical finding.
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> UsageHistoryList(string giftVoucherId, DataSourceRequest command)
    {
        var giftVoucher = await giftVoucherService.GetGiftVoucherById(giftVoucherId);
        if (giftVoucher == null || !await scope.CanView(giftVoucher))
            throw new ArgumentException("No gift voucher found with the specified id");

        var (giftVoucherUsageHistoryModels, totalCount) = await giftVoucherViewModelService
            .PrepareGiftVoucherUsageHistoryModels(giftVoucher, command.Page, command.PageSize);

        return Json(new DataSourceResult {
            Data = giftVoucherUsageHistoryModels.ToList(),
            Total = totalCount
        });
    }

    #endregion

    #region Shared helpers

    private IActionResult ApplyDefaultStore(GiftVoucherModel model)
    {
        return View(ApplyDefaultStoreToModel(model));
    }

    // Forces the current store onto a new/edited voucher and hides every other store from the
    // dropdown - a no-op for Admin (scope.DefaultStoreId is null), matches Store's original
    // SetCurrentStore helper exactly.
    private GiftVoucherModel ApplyDefaultStoreToModel(GiftVoucherModel model)
    {
        if (string.IsNullOrEmpty(scope.DefaultStoreId)) return model;
        model.StoreId = scope.DefaultStoreId;
        model.AvailableStores = model.AvailableStores.Where(x => x.Value == scope.DefaultStoreId).ToList();
        return model;
    }

    #endregion

    #region Gift card generation

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public IActionResult GenerateCouponCode()
    {
        return Json(new { CouponCode = giftVoucherService.GenerateGiftVoucherCode() });
    }

    #endregion
}
