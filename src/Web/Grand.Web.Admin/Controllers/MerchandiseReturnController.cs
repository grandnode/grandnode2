using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.MerchandiseReturns)]
    public partial class MerchandiseReturnController : BaseAdminController
    {
        #region Fields

        private readonly IMerchandiseReturnViewModelService _merchandiseReturnViewModelService;
        private readonly ITranslationService _translationService;
        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;

        #endregion Fields

        #region Constructors

        public MerchandiseReturnController(
            IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
            ITranslationService translationService,
            IMerchandiseReturnService merchandiseReturnService,
            IOrderService orderService,
            IWorkContext workContext,
            IGroupService groupService)
        {
            _merchandiseReturnViewModelService = merchandiseReturnViewModelService;
            _translationService = translationService;
            _merchandiseReturnService = merchandiseReturnService;
            _orderService = orderService;
            _workContext = workContext;
            _groupService = groupService;
        }

        #endregion

        #region Methods

        //list
        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = _merchandiseReturnViewModelService.PrepareReturnReqestListModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, MerchandiseReturnListModel model)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;
            }
            var merchandiseReturnModels = await _merchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = merchandiseReturnModels.merchandiseReturnModels,
                Total = merchandiseReturnModels.totalCount,
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> GoToId(MerchandiseReturnListModel model)
        {
            if (model.GoDirectlyToId == null)
                return RedirectToAction("List", "MerchandiseReturn");

            int.TryParse(model.GoDirectlyToId, out var id);

            //try to load a product entity
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(id);
            if (merchandiseReturn == null)
                //not found
                return RedirectToAction("List", "MerchandiseReturn");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && merchandiseReturn.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "MerchandiseReturn");
            }

            return RedirectToAction("Edit", "MerchandiseReturn", new { id = merchandiseReturn.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> ProductsForMerchandiseReturn(string merchandiseReturnId, DataSourceRequest command)
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
            if (merchandiseReturn == null)
                return ErrorForKendoGridJson("Merchandise return not found");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && merchandiseReturn.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return ErrorForKendoGridJson("Merchandise return is not your");
            }
            var items = await _merchandiseReturnViewModelService.PrepareMerchandiseReturnItemModel(merchandiseReturnId);
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = items.Count,
            };

            return Json(gridModel);
        }

        //edit
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(id);
            if (merchandiseReturn == null)
                //No merchandise return found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && merchandiseReturn.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "MerchandiseReturn");
            }

            //a vendor should have access only to his merchandise return
            if (_workContext.CurrentVendor != null && merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List", "MerchandiseReturn");

            var model = new MerchandiseReturnModel();
            await _merchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, merchandiseReturn, false);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(MerchandiseReturnModel model, bool continueEditing, IFormCollection form,
            [FromServices] IAddressAttributeService addressAttributeService,
            [FromServices] IAddressAttributeParser addressAttributeParser,
            [FromServices] OrderSettings orderSettings
            )
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(model.Id);
            if (merchandiseReturn == null)
                //No merchandise return found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && merchandiseReturn.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "MerchandiseReturn");
            }

            //a vendor should have access only to his merchandise return
            if (_workContext.CurrentVendor != null && merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List", "MerchandiseReturn");

            var customAddressAttributes = new List<CustomAttribute>();
            if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
            {
                customAddressAttributes = await form.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
                var customAddressAttributeWarnings = await addressAttributeParser.GetAttributeWarnings(customAddressAttributes);
                foreach (var error in customAddressAttributeWarnings)
                {
                    ModelState.AddModelError("", error);
                }
            }
            if (ModelState.IsValid)
            {
                merchandiseReturn = await _merchandiseReturnViewModelService.UpdateMerchandiseReturnModel(merchandiseReturn, model, customAddressAttributes);

                Success(_translationService.GetResource("Admin.Orders.MerchandiseReturns.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = merchandiseReturn.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            await _merchandiseReturnViewModelService.PrepareMerchandiseReturnModel(model, merchandiseReturn, false);
            return View(model);
        }

        //delete
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(id);
            if (merchandiseReturn == null)
                //No merchandise return found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && merchandiseReturn.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List", "MerchandiseReturn");
            }

            //a vendor can't delete merchandise return
            if (_workContext.CurrentVendor != null)
                return RedirectToAction("List", "MerchandiseReturn");

            if (ModelState.IsValid)
            {
                await _merchandiseReturnViewModelService.DeleteMerchandiseReturn(merchandiseReturn);
                Success(_translationService.GetResource("Admin.Orders.MerchandiseReturns.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = merchandiseReturn.Id });
        }

        #endregion

        #region Merchandise return notes

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> MerchandiseReturnNotesSelect(string merchandiseReturnId, DataSourceRequest command)
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
            if (merchandiseReturn == null)
                throw new ArgumentException("No merchandise return found with the specified id");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && merchandiseReturn.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Content("");
            }
            //a vendor should have access only to his merchandise return
            if (_workContext.CurrentVendor != null && merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
                return Content("");

            //merchandise return notes
            var merchandiseReturnNoteModels = await _merchandiseReturnViewModelService.PrepareMerchandiseReturnNotes(merchandiseReturn);
            var gridModel = new DataSourceResult
            {
                Data = merchandiseReturnNoteModels,
                Total = merchandiseReturnNoteModels.Count
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> MerchandiseReturnNoteAdd(string merchandiseReturnId, string orderId, string downloadId, bool displayToCustomer, string message)
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
            if (merchandiseReturn == null)
                return Json(new { Result = false });

            var order = await _orderService.GetOrderById(orderId);
            if (order == null)
                return Json(new { Result = false });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && merchandiseReturn.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Json(new { Result = false });
            }

            //a vendor should have access only to his merchandise return
            if (_workContext.CurrentVendor != null && merchandiseReturn.VendorId != _workContext.CurrentVendor.Id)
                return Json(new { Result = false });

            await _merchandiseReturnViewModelService.InsertMerchandiseReturnNote(merchandiseReturn, order, downloadId, displayToCustomer, message);

            return Json(new { Result = true });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> MerchandiseReturnNoteDelete(string id, string merchandiseReturnId)
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
            if (merchandiseReturn == null)
                throw new ArgumentException("No merchandise return found with the specified id");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Json(new { Result = false });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && merchandiseReturn.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Json(new { Result = false });
            }

            await _merchandiseReturnViewModelService.DeleteMerchandiseReturnNote(merchandiseReturn, id);

            return new JsonResult("");
        }
        #endregion
    }
}
