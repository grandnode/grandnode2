using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseCustomerAttributeController(
    ICustomerAttributeService customerAttributeService,
    ICustomerAttributeViewModelService customerAttributeViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<CustomerAttribute> scope) : BaseController
{
    #region Customer attributes

    public virtual IActionResult Index() => RedirectToAction("List");
    public virtual IActionResult List() => View();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public virtual async Task<IActionResult> List(DataSourceRequest command)
    {
        var customerAttributes = await customerAttributeViewModelService.PrepareCustomerAttributes();
        var storeId = scope.DefaultStoreId;
        var visible = storeId is null
            ? customerAttributes.ToList()
            : customerAttributes.Where(x => x.Stores == null || x.Stores.Length == 0 || x.Stores.Contains(storeId)).ToList();
        var gridModel = new DataSourceResult {
            Data = visible.Select(x => new {
                x.Id, x.Name, x.AttributeControlTypeName, x.IsRequired, x.DisplayOrder,
                IsGlobalAttribute = storeId is not null && !(x.Stores is { Length: 1 } && x.Stores.Contains(storeId))
            }).ToList(),
            Total = visible.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public virtual async Task<IActionResult> Create()
    {
        var model = customerAttributeViewModelService.PrepareCustomerAttributeModel();
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public virtual async Task<IActionResult> Create(CustomerAttributeModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var customerAttribute = await customerAttributeViewModelService.InsertCustomerAttributeModel(model);
            Success(translationService.GetResource("Admin.Customers.CustomerAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = customerAttribute.Id })
                : RedirectToAction("List");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public virtual async Task<IActionResult> Edit(string id)
    {
        var customerAttribute = await customerAttributeService.GetCustomerAttributeById(id);
        if (customerAttribute == null || !await scope.CanView(customerAttribute))
            return RedirectToAction("List");

        var model = customerAttributeViewModelService.PrepareCustomerAttributeModel(customerAttribute);
        model.IsGlobalAttribute = scope.DefaultStoreId is not null && !await scope.HasAccess(customerAttribute);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = customerAttribute.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> Edit(CustomerAttributeModel model, bool continueEditing)
    {
        var customerAttribute = await customerAttributeService.GetCustomerAttributeById(model.Id);
        if (customerAttribute == null || !await scope.HasAccess(customerAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            customerAttribute = await customerAttributeViewModelService.UpdateCustomerAttributeModel(model, customerAttribute);
            Success(translationService.GetResource("Admin.Customers.CustomerAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = customerAttribute.Id });
            }
            return RedirectToAction("List");
        }

        model.IsGlobalAttribute = false;
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = customerAttribute.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public virtual async Task<IActionResult> Delete(string id)
    {
        if (scope.DefaultStoreId is not null)
        {
            var customerAttribute = await customerAttributeService.GetCustomerAttributeById(id);
            if (customerAttribute == null)
                return RedirectToAction("List");
            if (!await scope.HasAccess(customerAttribute))
                return RedirectToAction("Edit", new { id });
        }

        await customerAttributeViewModelService.DeleteCustomerAttribute(id);
        Success(translationService.GetResource("Admin.Customers.CustomerAttributes.Deleted"));
        return RedirectToAction("List");
    }

    #endregion

    #region Customer attribute values

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public virtual async Task<IActionResult> ValueList(string customerAttributeId, DataSourceRequest command)
    {
        var customerAttribute = await customerAttributeService.GetCustomerAttributeById(customerAttributeId);
        if (customerAttribute == null || !await scope.CanView(customerAttribute))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var values = await customerAttributeViewModelService.PrepareCustomerAttributeValues(customerAttributeId);
        return Json(new DataSourceResult { Data = values.ToList(), Total = values.Count() });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueCreatePopup(string customerAttributeId)
    {
        var customerAttribute = await customerAttributeService.GetCustomerAttributeById(customerAttributeId);
        if (customerAttribute == null || !await scope.HasAccess(customerAttribute))
            return RedirectToAction("List");

        var model = customerAttributeViewModelService.PrepareCustomerAttributeValueModel(customerAttributeId);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueCreatePopup(CustomerAttributeValueModel model)
    {
        var customerAttribute = await customerAttributeService.GetCustomerAttributeById(model.CustomerAttributeId);
        if (customerAttribute == null || !await scope.HasAccess(customerAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await customerAttributeViewModelService.InsertCustomerAttributeValueModel(model);
            return Content("");
        }
        return View(model);
    }

    // Note: ValueEditPopup GET intentionally undecorated here as the permission action name diverges
    // between Admin (Preview) and Store (Edit). Each subclass overrides to apply its own
    // [PermissionAuthorizeAction] attribute.
    public virtual async Task<IActionResult> ValueEditPopup(string id, string customerAttributeId)
    {
        var customerAttribute = await customerAttributeService.GetCustomerAttributeById(customerAttributeId);
        if (customerAttribute == null || !await scope.HasAccess(customerAttribute))
            return RedirectToAction("List");

        var cav = customerAttribute.CustomerAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            return RedirectToAction("List");

        var model = customerAttributeViewModelService.PrepareCustomerAttributeValueModel(cav);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueEditPopup(CustomerAttributeValueModel model)
    {
        var customerAttribute = await customerAttributeService.GetCustomerAttributeById(model.CustomerAttributeId);
        if (customerAttribute == null || !await scope.HasAccess(customerAttribute))
            return RedirectToAction("List");

        var cav = customerAttribute.CustomerAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await customerAttributeViewModelService.UpdateCustomerAttributeValueModel(model, cav);
            return Content("");
        }
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueDelete(CustomerAttributeValueModel model)
    {
        if (scope.DefaultStoreId is not null)
        {
            var customerAttribute = await customerAttributeService.GetCustomerAttributeById(model.CustomerAttributeId);
            if (customerAttribute == null || !await scope.HasAccess(customerAttribute))
                return new JsonResult(new DataSourceResult { Errors = "Access denied" });
            if (customerAttribute.CustomerAttributeValues.All(x => x.Id != model.Id))
                return new JsonResult(new DataSourceResult
                    { Errors = "No customer attribute value found with the specified id" });
        }

        await customerAttributeViewModelService.DeleteCustomerAttributeValue(model);
        return new JsonResult("");
    }

    #endregion
}
