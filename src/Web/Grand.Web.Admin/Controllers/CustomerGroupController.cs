using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.CustomerGroups)]
public class CustomerGroupController : BaseAdminController
{
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

    #region Fields

    private readonly ICustomerGroupViewModelService _customerGroupViewModelService;
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly IPermissionService _permissionService;
    private readonly IWorkContext _workContext;
    private readonly ICustomerGroupProductService _customerGroupProductService;

    #endregion

    #region Customer groups

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        return View();
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var customerGroups = await _groupService.GetAllCustomerGroups(pageIndex: command.Page - 1,
            pageSize: command.PageSize, showHidden: true);
        var gridModel = new DataSourceResult {
            Data = customerGroups.Select(x =>
            {
                var rolesModel = x.ToModel();
                return rolesModel;
            }),
            Total = customerGroups.TotalCount
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
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(CustomerGroupModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var customerGroup = await _customerGroupViewModelService.InsertCustomerGroupModel(model);
            Success(_translationService.GetResource("Admin.Customers.CustomerGroups.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = customerGroup.Id })
                : RedirectToAction("List");
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
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
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
                customerGroup = await _customerGroupViewModelService.UpdateCustomerGroupModel(customerGroup, model);
                Success(_translationService.GetResource("Admin.Customers.CustomerGroups.Updated"));
                return continueEditing
                    ? RedirectToAction("Edit", new { id = customerGroup.Id })
                    : RedirectToAction("List");
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
    public async Task<IActionResult> Delete(CustomerGroupDeleteModel model)
    {
        var customerGroup = await _groupService.GetCustomerGroupById(model.Id);
        try
        {
            if (ModelState.IsValid)
            {
                await _customerGroupViewModelService.DeleteCustomerGroup(customerGroup);
                Success(_translationService.GetResource("Admin.Customers.CustomerGroups.Deleted"));
                return RedirectToAction("List");
            }

            Error(ModelState);
            return RedirectToAction("Edit", new { id = model.Id });
        }
        catch (Exception exc)
        {
            Error(exc.Message);
            return RedirectToAction("Edit", new { id = model.Id });
        }
    }

    #endregion

    #region Products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> Products(string customerGroupId, DataSourceRequest command)
    {
        var products = await _customerGroupViewModelService.PrepareCustomerGroupProductModel(customerGroupId);
        var gridModel = new DataSourceResult {
            Data = products,
            Total = products.Count
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
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command,
        CustomerGroupProductModel.AddProductModel model)
    {
        var products =
            await _customerGroupViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.products,
            Total = products.totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopup(CustomerGroupProductModel.AddProductModel model)
    {
        if (model.SelectedProductIds != null) await _customerGroupViewModelService.InsertProductModel(model);

        return Content("");
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
            model.Add(new CustomerGroupPermissionModel {
                Id = pr.Id,
                Name = pr.GetTranslationPermissionName(_translationService, _workContext),
                SystemName = pr.SystemName,
                Actions = pr.Actions.ToList(),
                Access = pr.CustomerGroups.Contains(customerGroupId)
            });

        var gridModel = new DataSourceResult {
            Data = model,
            Total = model.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AclUpdate(CustomerGroupAclUpdateModel model)
    {
        if (ModelState.IsValid)
        {
            var permissionRecord = await _permissionService.GetPermissionById(model.Id);
            if (model.Access)
            {
                if (!permissionRecord.CustomerGroups.Contains(model.CustomerGroupId))
                    permissionRecord.CustomerGroups.Add(model.CustomerGroupId);
            }
            else if (permissionRecord.CustomerGroups.Contains(model.CustomerGroupId))
            {
                permissionRecord.CustomerGroups.Remove(model.CustomerGroupId);
            }

            await _permissionService.UpdatePermission(permissionRecord);

            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}