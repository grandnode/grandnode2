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
