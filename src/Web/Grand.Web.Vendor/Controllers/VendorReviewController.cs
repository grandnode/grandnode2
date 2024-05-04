using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.VendorReview;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[PermissionAuthorize(PermissionSystemName.VendorReviews)]
public class VendorReviewController : BaseVendorController
{
    #region Constructors

    public VendorReviewController(
        IVendorReviewViewModelService vendorReviewViewModelService,
        IVendorService vendorService,
        ITranslationService translationService,
        IWorkContext workContext)
    {
        _vendorReviewViewModelService = vendorReviewViewModelService;
        _vendorService = vendorService;
        _translationService = translationService;
        _workContext = workContext;
    }

    #endregion

    #region Fields

    private readonly IVendorReviewViewModelService _vendorReviewViewModelService;
    private readonly IVendorService _vendorService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    #endregion Fields

    #region Methods

    //list
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
        var (vendorReviewModels, totalCount) =
            await _vendorReviewViewModelService.PrepareVendorReviewModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = vendorReviewModels.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var vendorReview = await _vendorService.GetVendorReviewById(id);

        if (vendorReview == null || vendorReview.VendorId != _workContext.CurrentVendor.Id)
            //No vendor review found with the specified id
            return RedirectToAction("List");

        var model = new VendorReviewModel();
        await _vendorReviewViewModelService.PrepareVendorReviewModel(model, vendorReview, false, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(VendorReviewModel model, bool continueEditing)
    {
        var vendorReview = await _vendorService.GetVendorReviewById(model.Id);
        if (vendorReview == null || vendorReview.VendorId != _workContext.CurrentVendor.Id)
            //No vendor review found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            vendorReview = await _vendorReviewViewModelService.UpdateVendorReviewModel(vendorReview, model);
            Success(_translationService.GetResource("Admin.VendorReviews.Updated"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = vendorReview.Id, vendorReview.VendorId })
                : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        await _vendorReviewViewModelService.PrepareVendorReviewModel(model, vendorReview, true, false);
        return View(model);
    }

    //delete
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var vendorReview = await _vendorService.GetVendorReviewById(id);
        if (vendorReview == null || vendorReview.VendorId != _workContext.CurrentVendor.Id)
            //No vendor review found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _vendorReviewViewModelService.DeleteVendorReview(vendorReview);

            Success(_translationService.GetResource("Admin.VendorReviews.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ApproveSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null) await _vendorReviewViewModelService.ApproveVendorReviews(selectedIds.ToList());

        return Json(new { Result = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> DisapproveSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null) await _vendorReviewViewModelService.DisapproveVendorReviews(selectedIds.ToList());

        return Json(new { Result = true });
    }

    #endregion
}