using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Admin.Models.Vendors;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.VendorReviews)]
    public partial class VendorReviewController : BaseAdminController
    {
        #region Fields

        private readonly IVendorViewModelService _vendorViewModelService;
        private readonly IVendorService _vendorService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;

        #endregion Fields

        #region Constructors

        public VendorReviewController(
            IVendorViewModelService vendorViewModelService,
            IVendorService vendorService,
            ITranslationService translationService,
            IGroupService groupService,
            IWorkContext workContext)
        {
            _vendorViewModelService = vendorViewModelService;
            _vendorService = vendorService;
            _translationService = translationService;
            _groupService = groupService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = new VendorReviewListModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, VendorReviewListModel model)
        {
            var vendorId = string.Empty;
            //vendor
            if (_workContext.CurrentVendor != null)
            {
                vendorId = _workContext.CurrentVendor.Id;
            }
            //admin
            else if (await _groupService.IsAdmin(_workContext.CurrentCustomer))
            {
                vendorId = model.SearchVendorId;
            }

            model.SearchVendorId = vendorId;
            var (vendorReviewModels, totalCount) = await _vendorViewModelService.PrepareVendorReviewModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult {
                Data = vendorReviewModels.ToList(),
                Total = totalCount,
            };

            return Json(gridModel);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var vendorReview = await _vendorService.GetVendorReviewById(id);

            if (vendorReview == null)
                //No vendor review found with the specified id
                return RedirectToAction("List");

            var model = new VendorReviewModel();
            await _vendorViewModelService.PrepareVendorReviewModel(model, vendorReview, false, false);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(VendorReviewModel model, bool continueEditing)
        {
            var vendorReview = await _vendorService.GetVendorReviewById(model.Id);
            if (vendorReview == null)
                //No vendor review found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                vendorReview = await _vendorViewModelService.UpdateVendorReviewModel(vendorReview, model);
                Success(_translationService.GetResource("Admin.VendorReviews.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = vendorReview.Id, VendorId = vendorReview.VendorId }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            await _vendorViewModelService.PrepareVendorReviewModel(model, vendorReview, true, false);
            return View(model);
        }

        //delete
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var vendorReview = await _vendorService.GetVendorReviewById(id);
            if (vendorReview == null)
                //No vendor review found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _vendorViewModelService.DeleteVendorReview(vendorReview);

                Success(_translationService.GetResource("Admin.VendorReviews.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = id });
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ApproveSelected(ICollection<string> selectedIds)
        {
            if (selectedIds != null)
            {
                await _vendorViewModelService.ApproveVendorReviews(selectedIds.ToList());
            }
            return Json(new { Result = true });
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> DisapproveSelected(ICollection<string> selectedIds)
        {
            if (selectedIds != null)
            {
                await _vendorViewModelService.DisapproveVendorReviews(selectedIds.ToList());
            }

            return Json(new { Result = true });
        }

        public async Task<IActionResult> VendorSearchAutoComplete(string term)
        {
            const int searchTermMinimumLength = 3;
            if (String.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
                return Content("");

            var vendors = await _vendorService.SearchVendors(
                keywords: term);

            var result = (from p in vendors
                          select new
                          {
                              label = p.Name,
                              vendorid = p.Id
                          })
                .ToList();
            return Json(result);
        }
        #endregion
    }
}