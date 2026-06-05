using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.GiftVouchers)]
public class GiftVoucherController : BaseStoreController
{
    private readonly IGiftVoucherViewModelService _giftVoucherViewModelService;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public GiftVoucherController(
        IGiftVoucherViewModelService giftVoucherViewModelService,
        IGiftVoucherService giftVoucherService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _giftVoucherViewModelService = giftVoucherViewModelService;
        _giftVoucherService = giftVoucherService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        var model = _giftVoucherViewModelService.PrepareGiftVoucherListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> GiftVoucherList(DataSourceRequest command, GiftVoucherListModel model)
    {
        var (giftVoucherModels, totalCount) =
            await _giftVoucherViewModelService.PrepareGiftVoucherModel(model, command.Page, command.PageSize,
                CurrentStoreId);

        return Json(new DataSourceResult {
            Data = giftVoucherModels.ToList(),
            Total = totalCount
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await PrepareStoreGiftVoucherModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(GiftVoucherModel model, bool continueEditing)
    {
        model.StoreId = CurrentStoreId;

        if (ModelState.IsValid)
        {
            var giftVoucher = await _giftVoucherViewModelService.InsertGiftVoucherModel(model);
            Success(_translationService.GetResource("Admin.GiftVouchers.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = giftVoucher.Id }) : RedirectToAction("List");
        }

        model = await PrepareStoreGiftVoucherModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var giftVoucher = await GetCurrentStoreGiftVoucher(id);
        if (giftVoucher == null)
            return RedirectToAction("List");

        var model = await _giftVoucherViewModelService.PrepareGiftVoucherModel(giftVoucher);
        model = SetCurrentStore(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(GiftVoucherModel model, bool continueEditing)
    {
        var giftVoucher = await GetCurrentStoreGiftVoucher(model.Id);
        if (giftVoucher == null)
            return RedirectToAction("List");

        model.StoreId = CurrentStoreId;
        await _giftVoucherViewModelService.FillGiftVoucherModel(giftVoucher, model);

        if (ModelState.IsValid)
        {
            giftVoucher = await _giftVoucherViewModelService.UpdateGiftVoucherModel(giftVoucher, model);
            Success(_translationService.GetResource("Admin.GiftVouchers.Updated"));

            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = giftVoucher.Id });
            }

            return RedirectToAction("List");
        }

        model = SetCurrentStore(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public IActionResult GenerateCouponCode()
    {
        return Json(new { CouponCode = _giftVoucherService.GenerateGiftVoucherCode() });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> NotifyRecipient(GiftVoucherNotifyRecipient model)
    {
        var giftVoucher = await GetCurrentStoreGiftVoucher(model.Id);
        if (giftVoucher == null)
            return RedirectToAction("List");

        try
        {
            if (ModelState.IsValid)
                await _giftVoucherViewModelService.NotifyRecipient(giftVoucher);
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
        var giftVoucher = await GetCurrentStoreGiftVoucher(model.Id);
        if (giftVoucher == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _giftVoucherViewModelService.DeleteGiftVoucher(giftVoucher);
            Success(_translationService.GetResource("Admin.GiftVouchers.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = giftVoucher.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> UsageHistoryList(string giftVoucherId, DataSourceRequest command)
    {
        var giftVoucher = await GetCurrentStoreGiftVoucher(giftVoucherId);
        if (giftVoucher == null)
            throw new ArgumentException("No gift voucher found with the specified id");

        var (giftVoucherUsageHistoryModels, totalCount) =
            await _giftVoucherViewModelService.PrepareGiftVoucherUsageHistoryModels(giftVoucher, command.Page,
                command.PageSize);

        return Json(new DataSourceResult {
            Data = giftVoucherUsageHistoryModels.ToList(),
            Total = totalCount
        });
    }

    private async Task<GiftVoucherModel> PrepareStoreGiftVoucherModel(GiftVoucherModel model = null)
    {
        model = await _giftVoucherViewModelService.PrepareGiftVoucherModel(model);
        return SetCurrentStore(model);
    }

    private GiftVoucherModel SetCurrentStore(GiftVoucherModel model)
    {
        model.StoreId = CurrentStoreId;
        model.AvailableStores = model.AvailableStores.Where(x => x.Value == CurrentStoreId).ToList();
        return model;
    }

    private async Task<GiftVoucher> GetCurrentStoreGiftVoucher(string id)
    {
        var giftVoucher = await _giftVoucherService.GetGiftVoucherById(id);
        return giftVoucher?.StoreId == CurrentStoreId ? giftVoucher : null;
    }
}
