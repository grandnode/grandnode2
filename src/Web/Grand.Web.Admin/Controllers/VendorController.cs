using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Commands.Models;
using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Vendors;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Vendors)]
    public partial class VendorController : BaseAdminController
    {
        #region Fields

        private readonly IVendorViewModelService _vendorViewModelService;
        private readonly ITranslationService _translationService;
        private readonly IVendorService _vendorService;
        private readonly ILanguageService _languageService;
        private readonly IMediator _mediator;

        #endregion

        #region Constructors

        public VendorController(
            IVendorViewModelService vendorViewModelService,
            ITranslationService translationService,
            IVendorService vendorService,
            ILanguageService languageService,
            IMediator mediator)
        {
            _vendorViewModelService = vendorViewModelService;
            _translationService = translationService;
            _vendorService = vendorService;
            _languageService = languageService;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = new VendorListModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, VendorListModel model)
        {
            var vendors = await _vendorService.GetAllVendors(model.SearchName, command.Page - 1, command.PageSize, true);
            var gridModel = new DataSourceResult
            {
                Data = vendors.Select(x =>
                {
                    var vendorModel = x.ToModel();
                    return vendorModel;
                }),
                Total = vendors.TotalCount,
            };
            return Json(gridModel);
        }

        //create
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = await _vendorViewModelService.PrepareVendorModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(VendorModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var vendor = await _vendorViewModelService.InsertVendorModel(model);
                Success(_translationService.GetResource("Admin.Vendors.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = vendor.Id }) : RedirectToAction("List");
            }
            //prepare address model
            await _vendorViewModelService.PrepareVendorAddressModel(model, null);
            //discounts
            await _vendorViewModelService.PrepareDiscountModel(model, null, true);
            //stores
            await _vendorViewModelService.PrepareStore(model);

            //If we got this far, something failed, redisplay form
            return View(model);
        }


        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var vendor = await _vendorService.GetVendorById(id);
            if (vendor == null || vendor.Deleted)
                //No vendor found with the specified id
                return RedirectToAction("List");

            var model = vendor.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = vendor.GetTranslation(x => x.Name, languageId, false);
                locale.Description = vendor.GetTranslation(x => x.Description, languageId, false);
                locale.MetaKeywords = vendor.GetTranslation(x => x.MetaKeywords, languageId, false);
                locale.MetaDescription = vendor.GetTranslation(x => x.MetaDescription, languageId, false);
                locale.MetaTitle = vendor.GetTranslation(x => x.MetaTitle, languageId, false);
                locale.SeName = vendor.GetSeName(languageId, false);
            });
            //discounts
            await _vendorViewModelService.PrepareDiscountModel(model, vendor, false);
            //associated customer emails
            model.AssociatedCustomers = await _vendorViewModelService.AssociatedCustomers(vendor.Id);
            //prepare address model
            await _vendorViewModelService.PrepareVendorAddressModel(model, vendor);
            //stores
            await _vendorViewModelService.PrepareStore(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(VendorModel model, bool continueEditing)
        {
            var vendor = await _vendorService.GetVendorById(model.Id);
            if (vendor == null || vendor.Deleted)
                //No vendor found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                vendor = await _vendorViewModelService.UpdateVendorModel(vendor, model);

                Success(_translationService.GetResource("Admin.Vendors.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = vendor.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //discounts
            await _vendorViewModelService.PrepareDiscountModel(model, vendor, true);
            //prepare address model
            await _vendorViewModelService.PrepareVendorAddressModel(model, vendor);
            //associated customer emails
            model.AssociatedCustomers = await _vendorViewModelService.AssociatedCustomers(vendor.Id);
            //stores
            await _vendorViewModelService.PrepareStore(model);

            return View(model);
        }


        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> Activate(string id, string activatevendor)
        {
            var vendor = await _vendorService.GetVendorById(id);
            if (vendor == null || vendor.Deleted)
                //No vendor found with the specified id
                return RedirectToAction("List");

            var associatedCustomers = await _vendorViewModelService.AssociatedCustomers(vendor.Id);
            if (!associatedCustomers.Any())
            {
                Error(_translationService.GetResource("Admin.Vendors.Fields.AssociatedCustomerEmails.None"));
                return RedirectToAction("Edit", new { id = vendor.Id });
            }

            var activate = await _mediator.Send(new ActiveVendorCommand()
            {
                Vendor = vendor,
                Active = activatevendor == "active",
                CustomerIds = associatedCustomers.Select(x => x.Id).ToList()
            });

            if (activate)
                Success(_translationService.GetResource("Admin.Vendors.Updated"));

            return RedirectToAction("Edit", new { id = vendor.Id });

        }

        //delete
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var vendor = await _vendorService.GetVendorById(id);
            if (vendor == null)
                //No vendor found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _vendorViewModelService.DeleteVendor(vendor);
                Success(_translationService.GetResource("Admin.Vendors.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = vendor.Id });
        }

        #endregion

        #region Vendor notes

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> VendorNotesSelect(string vendorId, DataSourceRequest command)
        {
            var vendor = await _vendorService.GetVendorById(vendorId);
            if (vendor == null)
                throw new ArgumentException("No vendor found with the specified id");

            var vendorNoteModels = _vendorViewModelService.PrepareVendorNote(vendor);
            var gridModel = new DataSourceResult
            {
                Data = vendorNoteModels,
                Total = vendorNoteModels.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> VendorNoteAdd(string vendorId, string message)
        {
            if (ModelState.IsValid)
            {
                var result = await _vendorViewModelService.InsertVendorNote(vendorId, message);
                return Json(new { Result = result });
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> VendorNoteDelete(string id, string vendorId)
        {
            if (ModelState.IsValid)
            {
                await _vendorViewModelService.DeleteVendorNote(id, vendorId);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        #endregion

        #region Reviews

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> Reviews(DataSourceRequest command, string vendorId, [FromServices] IWorkContext workContext)
        {
            var vendor = await _vendorService.GetVendorById(vendorId);
            if (vendor == null)
                throw new ArgumentException("No vendor found with the specified id");

            //a vendor should have access only to his own profile
            if (workContext.CurrentVendor != null && vendor.Id != workContext.CurrentVendor.Id)
                return Content("This is not your vendor");

            var vendorReviews = await _vendorService.GetAllVendorReviews("", null,
                null, null, "", vendorId, command.Page - 1, command.PageSize);
            var items = new List<VendorReviewModel>();
            foreach (var item in vendorReviews)
            {
                var m = new VendorReviewModel();
                await _vendorViewModelService.PrepareVendorReviewModel(m, item, false, true);
                items.Add(m);
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = vendorReviews.TotalCount,
            };

            return Json(gridModel);
        }

        #endregion

    }
}
