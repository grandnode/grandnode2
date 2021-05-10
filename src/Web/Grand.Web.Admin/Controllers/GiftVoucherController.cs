using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.GiftVouchers)]
    public partial class GiftVoucherController : BaseAdminController
    {
        #region Fields
        private readonly IGiftVoucherViewModelService _giftVoucherViewModelService;
        private readonly IGiftVoucherService _giftVoucherService;
        private readonly ITranslationService _translationService;
        #endregion

        #region Constructors

        public GiftVoucherController(
            IGiftVoucherViewModelService giftVoucherViewModelService,
            IGiftVoucherService giftVoucherService,
            ITranslationService translationService)
        {
            _giftVoucherViewModelService = giftVoucherViewModelService;
            _giftVoucherService = giftVoucherService;
            _translationService = translationService;
        }

        #endregion

        #region Methods

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = _giftVoucherViewModelService.PrepareGiftVoucherListModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> GiftVoucherList(DataSourceRequest command, GiftVoucherListModel model)
        {
            var (giftVoucherModels, totalCount) = await _giftVoucherViewModelService.PrepareGiftVoucherModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = giftVoucherModels.ToList(),
                Total = totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = await _giftVoucherViewModelService.PrepareGiftVoucherModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(GiftVoucherModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var giftVoucher = await _giftVoucherViewModelService.InsertGiftVoucherModel(model);
                Success(_translationService.GetResource("Admin.GiftVouchers.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = giftVoucher.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            model = await _giftVoucherViewModelService.PrepareGiftVoucherModel(model);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(id);
            if (giftVoucher == null)
                //No gift voucher found with the specified id
                return RedirectToAction("List");

            var model = await _giftVoucherViewModelService.PrepareGiftVoucherModel(giftVoucher);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(GiftVoucherModel model, bool continueEditing)
        {
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(model.Id);
            if (giftVoucher == null)
                return RedirectToAction("List");

            await _giftVoucherViewModelService.FillGiftVoucherModel(giftVoucher, model);

            if (ModelState.IsValid)
            {
                giftVoucher = await _giftVoucherViewModelService.UpdateGiftVoucherModel(giftVoucher, model);
                Success(_translationService.GetResource("Admin.GiftVouchers.Updated"));

                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = giftVoucher.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
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
        public async Task<IActionResult> NotifyRecipient(GiftVoucherModel model)
        {
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(model.Id);

            if (!CommonHelper.IsValidEmail(giftVoucher.RecipientEmail))
                ModelState.AddModelError("", "Recipient email is not valid");
            if (!CommonHelper.IsValidEmail(giftVoucher.SenderEmail))
                ModelState.AddModelError("", "Sender email is not valid");

            try
            {
                if (ModelState.IsValid)
                {
                    await _giftVoucherViewModelService.NotifyRecipient(giftVoucher, model);
                }
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
        public async Task<IActionResult> Delete(string id)
        {
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(id);
            if (giftVoucher == null)
                //No gift voucher found with the specified id
                return RedirectToAction("List");

            if (giftVoucher.GiftVoucherUsageHistory.Any())
                ModelState.AddModelError("", _translationService.GetResource("Admin.GiftVouchers.PreventDeleted"));

            if (ModelState.IsValid)
            {
                await _giftVoucherViewModelService.DeleteGiftVoucher(giftVoucher);
                Success(_translationService.GetResource("Admin.GiftVouchers.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = giftVoucher.Id });
        }

        //Gif card usage history

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> UsageHistoryList(string giftVoucherId, DataSourceRequest command)
        {
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(giftVoucherId);
            if (giftVoucher == null)
                throw new ArgumentException("No gift voucher found with the specified id");

            var (giftVoucherUsageHistoryModels, totalCount) = await _giftVoucherViewModelService.PrepareGiftVoucherUsageHistoryModels(giftVoucher, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = giftVoucherUsageHistoryModels.ToList(),
                Total = totalCount
            };

            return Json(gridModel);
        }

        #endregion
    }
}
