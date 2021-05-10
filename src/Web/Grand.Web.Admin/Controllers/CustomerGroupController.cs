using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Customers;
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
    [PermissionAuthorize(PermissionSystemName.CustomerGroups)]
    public partial class CustomerGroupController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerGroupViewModelService _customerGroupViewModelService;
        private readonly IGroupService _groupService;
        private readonly ITranslationService _translationService;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerGroupProductService _customerGroupProductService;

        #endregion

        #region Constructors

        public CustomerGroupController(
            ICustomerGroupViewModelService customerGroupViewModelService,
            IGroupService groupService,
            ITranslationService translationService,
            IPermissionService permissionService,
            IWorkContext workContext,
            ICustomerGroupProductService customerGroupProductService)
        {
            _customerGroupViewModelService = customerGroupViewModelService;
            _groupService = groupService;
            _translationService = translationService;
            _permissionService = permissionService;
            _workContext = workContext;
            _customerGroupProductService = customerGroupProductService;
        }

        #endregion

        #region Customer groups

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var customerGroups = await _groupService.GetAllCustomerGroups(pageIndex: command.Page - 1, pageSize: command.PageSize, showHidden: true);
            var gridModel = new DataSourceResult
            {
                Data = customerGroups.Select(x =>
                {
                    var rolesModel = x.ToModel();
                    return rolesModel;
                }),
                Total = customerGroups.TotalCount,
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public IActionResult Create()
        {
            var model = _customerGroupViewModelService.PrepareCustomerGroupModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(CustomerGroupModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var customerGroup = await _customerGroupViewModelService.InsertCustomerGroupModel(model);
                Success(_translationService.GetResource("Admin.Customers.CustomerGroups.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = customerGroup.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var customerGroup = await _groupService.GetCustomerGroupById(id);
            if (customerGroup == null)
                //No customer group found with the specified id
                return RedirectToAction("List");

            var model = _customerGroupViewModelService.PrepareCustomerGroupModel(customerGroup);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(CustomerGroupModel model, bool continueEditing)
        {
            var customerGroup = await _groupService.GetCustomerGroupById(model.Id);
            if (customerGroup == null)
                //No customer group found with the specified id
                return RedirectToAction("List");

            try
            {
                if (ModelState.IsValid)
                {
                    if (customerGroup.IsSystem && !model.Active)
                        throw new GrandException(_translationService.GetResource("Admin.Customers.CustomerGroups.Fields.Active.CantEditSystem"));

                    if (customerGroup.IsSystem && !customerGroup.SystemName.Equals(model.SystemName, StringComparison.OrdinalIgnoreCase))
                        throw new GrandException(_translationService.GetResource("Admin.Customers.CustomerGroups.Fields.SystemName.CantEditSystem"));

                    customerGroup = await _customerGroupViewModelService.UpdateCustomerGroupModel(customerGroup, model);
                    Success(_translationService.GetResource("Admin.Customers.CustomerGroups.Updated"));
                    return continueEditing ? RedirectToAction("Edit", new { id = customerGroup.Id }) : RedirectToAction("List");
                }

                //If we got this far, something failed, redisplay form
                return View(model);
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("Edit", new { id = customerGroup.Id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var customerGroup = await _groupService.GetCustomerGroupById(id);
            if (customerGroup == null)
                //No customer group found with the specified id
                return RedirectToAction("List");
            if (customerGroup.IsSystem)
                ModelState.AddModelError("", "You can't delete system group");
            try
            {
                if (ModelState.IsValid)
                {
                    await _customerGroupViewModelService.DeleteCustomerGroup(customerGroup);
                    Success(_translationService.GetResource("Admin.Customers.CustomerGroups.Deleted"));
                    return RedirectToAction("List");
                }
                Error(ModelState);
                return RedirectToAction("Edit", new { id = customerGroup.Id });
            }
            catch (Exception exc)
            {
                Error(exc.Message);
                return RedirectToAction("Edit", new { id = customerGroup.Id });
            }
        }
        #endregion

        #region Products
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> Products(string customerGroupId, DataSourceRequest command)
        {
            var products = await _customerGroupViewModelService.PrepareCustomerGroupProductModel(customerGroupId);
            var gridModel = new DataSourceResult
            {
                Data = products,
                Total = products.Count()
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductDelete(string id)
        {
            var crp = await _customerGroupProductService.GetCustomerGroupProductById(id);
            if (crp == null)
                throw new ArgumentException("No found the specified id");
            if (ModelState.IsValid)
            {
                await _customerGroupProductService.DeleteCustomerGroupProduct(crp);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductUpdate(CustomerGroupProductModel model)
        {
            var crp = await _customerGroupProductService.GetCustomerGroupProductById(model.Id);
            if (crp == null)
                throw new ArgumentException("No customer group product found with the specified id");
            if (ModelState.IsValid)
            {
                crp.DisplayOrder = model.DisplayOrder;
                await _customerGroupProductService.UpdateCustomerGroupProduct(crp);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductAddPopup(string customerGroupId)
        {
            var model = await _customerGroupViewModelService.PrepareProductModel(customerGroupId);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CustomerGroupProductModel.AddProductModel model)
        {
            var products = await _customerGroupViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = products.products,
                Total = products.totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopup(CustomerGroupProductModel.AddProductModel model)
        {
            if (model.SelectedProductIds != null)
            {
                await _customerGroupViewModelService.InsertProductModel(model);
            }

            //a vendor should have access only to his products
            ViewBag.RefreshPage = true;
            return View(model);
        }
        #endregion

        #region Acl

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> Acl(string customerGroupId)
        {
            var permissionRecords = await _permissionService.GetAllPermissions();
            var model = new List<CustomerGroupPermissionModel>();

            foreach (var pr in permissionRecords)
            {
                model.Add(new CustomerGroupPermissionModel
                {
                    Id = pr.Id,
                    Name = pr.GetTranslationPermissionName(_translationService, _workContext),
                    SystemName = pr.SystemName,
                    Actions = pr.Actions.ToList(),
                    Access = pr.CustomerGroups.Contains(customerGroupId)
                });
            }

            var gridModel = new DataSourceResult
            {
                Data = model,
                Total = model.Count()
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> AclUpdate(string customerGroupId, string id, bool access)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageAcl))
                ModelState.AddModelError("", "You don't have permission to the update");

            var cr = await _groupService.GetCustomerGroupById(customerGroupId);
            if (cr == null)
                throw new ArgumentException("No customer group found with the specified id");

            var permissionRecord = await _permissionService.GetPermissionById(id);
            if (permissionRecord == null)
                throw new ArgumentException("No permission found with the specified id");

            if (ModelState.IsValid)
            {
                if (access)
                {
                    if (!permissionRecord.CustomerGroups.Contains(customerGroupId))
                        permissionRecord.CustomerGroups.Add(customerGroupId);
                }
                else
                    if (permissionRecord.CustomerGroups.Contains(customerGroupId))
                    permissionRecord.CustomerGroups.Remove(customerGroupId);

                await _permissionService.UpdatePermission(permissionRecord);

                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }


        #endregion
    }
}
