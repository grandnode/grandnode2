using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Permissions;
using Grand.Mediator;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Discounts)]
[AutoValidateAntiforgeryToken]
public abstract class BaseDiscountController(
    IDiscountViewModelService discountViewModelService,
    IDiscountService discountService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IAdminDataScope<Discount> scope)
    : BaseController
{
    #region Discounts

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        var model = discountViewModelService.PrepareDiscountListModel();
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DiscountListModel model, DataSourceRequest command)
    {
        var (discountModel, totalCount) =
            await discountViewModelService.PrepareDiscountModel(model, command.Page, command.PageSize);
        return Json(new DataSourceResult { Data = discountModel.ToList(), Total = totalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new DiscountModel();
        await discountViewModelService.PrepareDiscountModel(model, null);
        model.LimitationTimes = 1;
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(DiscountModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var discount = await discountViewModelService.InsertDiscountModel(model);
            Success(translationService.GetResource("admin.marketing.discounts.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = discount.Id }) : RedirectToAction("List");
        }

        await discountViewModelService.PrepareDiscountModel(model, null);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var discount = await discountService.GetDiscountById(id);
        if (discount == null || !await scope.CanView(discount))
            return RedirectToAction("List");

        var model = discount.ToModel(dateTimeService);
        await discountViewModelService.PrepareDiscountModel(model, discount);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(DiscountModel model, bool continueEditing)
    {
        var discount = await discountService.GetDiscountById(model.Id);
        if (discount == null)
            return RedirectToAction("List");

        if (!await scope.HasAccess(discount))
            return RedirectToAction("Edit", new { id = discount.Id });

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            discount = await discountViewModelService.UpdateDiscountModel(discount, model);
            Success(translationService.GetResource("admin.marketing.discounts.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = discount.Id });
            }

            return RedirectToAction("List");
        }

        await discountViewModelService.PrepareDiscountModel(model, discount);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var discount = await discountService.GetDiscountById(id);
        if (discount == null)
            return RedirectToAction("List");

        if (!await scope.HasAccess(discount))
            return RedirectToAction("Edit", new { id = discount.Id });

        var usageHistory = await mediator.Send(new GetDiscountUsageHistoryQuery { DiscountId = discount.Id });
        if (usageHistory.Count > 0)
        {
            Error(translationService.GetResource("admin.marketing.discounts.Deleted.UsageHistory"));
            return RedirectToAction("Edit", new { id = discount.Id });
        }

        if (ModelState.IsValid)
        {
            await discountViewModelService.DeleteDiscount(discount);
            Success(translationService.GetResource("admin.marketing.discounts.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = discount.Id });
    }

    #endregion

    #region Discount coupon codes

    [HttpPost]
    public async Task<IActionResult> CouponCodeList(DataSourceRequest command, string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var couponcodes = await discountService.GetAllCouponCodesByDiscountId(discount.Id,
            command.Page - 1, command.PageSize);
        return Json(new DataSourceResult {
            Data = couponcodes.Select(x => new { x.Id, x.CouponCode, x.Used }),
            Total = couponcodes.TotalCount
        });
    }

    public async Task<IActionResult> CouponCodeDelete(string discountId, string id)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var coupon = await discountService.GetDiscountCodeById(id);
        if (coupon == null)
            throw new Exception("No coupon code found with the specified id");
        if (ModelState.IsValid)
        {
            if (!coupon.Used)
                await discountService.DeleteDiscountCoupon(coupon);
            else
                return new JsonResult(new DataSourceResult { Errors = "You can't delete coupon code, it was used" });
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    public async Task<IActionResult> CouponCodeInsert(string discountId, string couponCode)
    {
        if (string.IsNullOrEmpty(couponCode))
            throw new Exception("Coupon code can't be empty");

        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        couponCode = couponCode.ToUpperInvariant();

        if (await discountService.GetDiscountByCouponCode(couponCode) != null)
            return new JsonResult(new DataSourceResult { Errors = "Coupon code exists" });
        if (ModelState.IsValid)
        {
            await discountViewModelService.InsertCouponCode(discountId, couponCode);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
