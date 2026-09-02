using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Discounts;
using Grand.Domain.Permissions;
using Grand.Domain.Vendors;
using Grand.Mediator;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Concrete host subclass of BaseDiscountController (ARCH-001 Discount consolidation). This class
// supplies Admin's DI wiring plus the attributes that used to arrive transitively via
// BaseAdminController - BaseDiscountController can't inherit any single host's base controller
// (it's shared across Admin/Store, each with a different [Area]/[Authorize*] pair), so each
// subclass restates its own host's attribute set explicitly, same pattern as OrderController and
// MerchandiseReturnController.
//
// Unlike Store, Admin also carries the "Applied to vendors" region below: Store's original
// DiscountController never had vendor actions/views, so they stay on this concrete subclass
// rather than moving into BaseDiscountController. Task 7b added these 5 actions to Admin's
// then-still-full controller with no discount-scope access check at all (unlike every other
// region's guard). This task (ARCH-001 Task 9) folds them onto the injected IAdminDataScope<Discount>
// scope, matching the HasAccess/CanView split used by every other region in BaseDiscountController
// (VendorList -> CanView, the four mutating actions -> HasAccess) - this is new access control being
// added here, not a preserved behavior.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class DiscountController(
    IDiscountViewModelService discountViewModelService,
    IDiscountService discountService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IDiscountProviderLoader discountProviderLoader,
    IAdminDataScope<Discount> scope)
    : BaseDiscountController(discountViewModelService, discountService, translationService, dateTimeService,
        mediator, discountProviderLoader, scope)
{
    #region Applied to vendors (Admin-only — no Store equivalent, see Task 7b)

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> VendorList(DataSourceRequest command, string discountId,
        [FromServices] IVendorService vendorService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.CanView(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var vendors = await vendorService.GetAllVendorsByDiscount(discount.Id);
        return Json(new DataSourceResult {
            Data = vendors.Select(x => new DiscountModel.AppliedToVendorModel { VendorId = x.Id, VendorName = x.Name }),
            Total = vendors.Count
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> VendorDelete(string discountId, string vendorId,
        [FromServices] IVendorService vendorService)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var vendor = await vendorService.GetVendorById(vendorId);
        if (vendor == null)
            throw new Exception("No vendor found with the specified id");
        if (ModelState.IsValid)
        {
            await discountViewModelService.DeleteVendor(discount, vendor);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> VendorAddPopup(string discountId)
    {
        var discount = await discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        return View(new DiscountModel.AddVendorToDiscountModel());
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> VendorAddPopupList(DataSourceRequest command,
        DiscountModel.AddVendorToDiscountModel model, [FromServices] IVendorService vendorService)
    {
        var vendors = await vendorService.GetAllVendors(model.SearchVendorName, command.Page - 1, command.PageSize, true);

        if (!string.IsNullOrEmpty(model.SearchVendorEmail))
        {
            var tempVendors = vendors.Where(x => x.Email.ToLowerInvariant().Contains(model.SearchVendorEmail.Trim()));
            vendors = new PagedList<Vendor>(tempVendors, command.Page - 1, command.PageSize);
        }

        return Json(new DataSourceResult { Data = vendors.Select(x => x.ToModel()), Total = vendors.TotalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> VendorAddPopup(DiscountModel.AddVendorToDiscountModel model)
    {
        var discount = await discountService.GetDiscountById(model.DiscountId);
        if (discount == null)
            throw new Exception("No discount found with the specified id");
        if (!await scope.HasAccess(discount))
            return Content("Access denied");

        if (model.SelectedVendorIds != null) await discountViewModelService.InsertVendorToDiscountModel(model);
        return Content("");
    }

    #endregion
}
