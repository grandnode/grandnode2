using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Vendors;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Vendors;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.VendorReviews)]
[AutoValidateAntiforgeryToken]
public abstract class BaseVendorReviewController(
    IVendorViewModelService vendorViewModelService,
    IVendorService vendorService,
    ITranslationService translationService,
    IAdminDataScope<VendorReview> scope)
    : BaseController
{
    // Exposed for host-specific concrete subclasses (Admin's VendorSearchAutoComplete action needs
    // vendorService — primary-constructor parameters aren't visible to derived classes by name in
    // C#).
    protected IVendorViewModelService VendorViewModelService => vendorViewModelService;
    protected IVendorService VendorService => vendorService;
    protected ITranslationService TranslationService => translationService;
    protected IAdminDataScope<VendorReview> Scope => scope;

    /// <summary>DRY replacement for the repeated "load vendor review, redirect to List if not found
    /// or not authorized" pattern found in both original controllers. Not a behavior change — every
    /// call site below still individually returns RedirectToAction("List") exactly as the
    /// originals did.</summary>
    private async Task<(VendorReview vendorReview, IActionResult denied)> LoadAuthorizedVendorReview(string id)
    {
        var vendorReview = await vendorService.GetVendorReviewById(id);
        if (vendorReview == null) return (null, RedirectToAction("List"));
        if (!await scope.HasAccess(vendorReview)) return (null, RedirectToAction("List"));
        return (vendorReview, null);
    }

    #region VendorReviews

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        var model = new VendorReviewListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, VendorReviewListModel model)
    {
        // Scope forces the caller's SearchVendorId only when the host is vendor-scoped (mirrors
        // BaseShipmentController.ShipmentListSelect's identical idiom): Admin (DefaultVendorId ==
        // null) passes through whatever the caller/picker supplied; Vendor (DefaultVendorId ==
        // CurrentVendor.Id) always forces its own id, reproducing today's hardcoded behavior.
        if (scope.DefaultVendorId is not null) model.SearchVendorId = scope.DefaultVendorId;

        var (vendorReviewModels, totalCount) =
            await vendorViewModelService.PrepareVendorReviewModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = vendorReviewModels.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var (vendorReview, denied) = await LoadAuthorizedVendorReview(id);
        if (denied != null) return denied;

        var model = new VendorReviewModel();
        await vendorViewModelService.PrepareVendorReviewModel(model, vendorReview, false, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(VendorReviewModel model, bool continueEditing)
    {
        var (vendorReview, denied) = await LoadAuthorizedVendorReview(model.Id);
        if (denied != null) return denied;

        if (ModelState.IsValid)
        {
            vendorReview = await vendorViewModelService.UpdateVendorReviewModel(vendorReview, model);
            Success(translationService.GetResource("Admin.VendorReviews.Updated"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = vendorReview.Id, vendorReview.VendorId })
                : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        await vendorViewModelService.PrepareVendorReviewModel(model, vendorReview, true, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var (vendorReview, denied) = await LoadAuthorizedVendorReview(id);
        if (denied != null) return denied;

        if (ModelState.IsValid)
        {
            await vendorViewModelService.DeleteVendorReview(vendorReview);

            Success(translationService.GetResource("Admin.VendorReviews.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ApproveSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null) await vendorViewModelService.ApproveVendorReviews(selectedIds.ToList(), scope);
        return Json(new { Result = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> DisapproveSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null) await vendorViewModelService.DisapproveVendorReviews(selectedIds.ToList(), scope);

        return Json(new { Result = true });
    }

    #endregion
}
