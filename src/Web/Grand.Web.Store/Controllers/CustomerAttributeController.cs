using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Store.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.CustomerAttributes)]
public class CustomerAttributeController : BaseStoreController
{
    private readonly ICustomerAttributeViewModelService _customerAttributeViewModelService;
    private readonly ICustomerAttributeService _customerAttributeService;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public CustomerAttributeController(
        ICustomerAttributeViewModelService customerAttributeViewModelService,
        ICustomerAttributeService customerAttributeService,
        ILanguageService languageService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _customerAttributeViewModelService = customerAttributeViewModelService;
        _customerAttributeService = customerAttributeService;
        _languageService = languageService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    private bool IsVisibleToStore(CustomerAttribute attr) =>
        !attr.LimitedToStores || attr.Stores.Contains(CurrentStoreId);

    #region Customer attributes

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var storeId = CurrentStoreId;
        var customerAttributes = (await _customerAttributeViewModelService.PrepareCustomerAttributes())
            .Where(x => x.Stores == null || x.Stores.Length == 0 || x.Stores.Contains(storeId))
            .ToList();
        var gridModel = new DataSourceResult {
            Data = customerAttributes.Select(x => new {
                x.Id,
                x.Name,
                x.AttributeControlTypeName,
                x.IsRequired,
                x.DisplayOrder,
                IsGlobalAttribute = !(x.Stores is { Length: 1 } && x.Stores.Contains(storeId))
            }).ToList(),
            Total = customerAttributes.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new CustomerAttributeStoreModel();
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(CustomerAttributeStoreModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            model.Stores = [CurrentStoreId];
            var customerAttribute = await _customerAttributeViewModelService.InsertCustomerAttributeModel(model);
            Success(_translationService.GetResource("Admin.Customers.CustomerAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = customerAttribute.Id })
                : RedirectToAction("List");
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(id);
        if (customerAttribute == null || !IsVisibleToStore(customerAttribute))
            return RedirectToAction("List");

        var model = customerAttribute.MapTo<CustomerAttribute, CustomerAttributeStoreModel>();
        model.IsGlobalAttribute = !customerAttribute.AccessToEntityByStore(CurrentStoreId);
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = customerAttribute.GetTranslation(x => x.Name, languageId, false);
        });

        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(CustomerAttributeModel model, bool continueEditing)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(model.Id);
        if (customerAttribute == null)
            return RedirectToAction("List");

        if (!customerAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            model.Stores = [CurrentStoreId];
            customerAttribute =
                await _customerAttributeViewModelService.UpdateCustomerAttributeModel(model, customerAttribute);
            Success(_translationService.GetResource("Admin.Customers.CustomerAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = customerAttribute.Id });
            }

            return RedirectToAction("List");
        }

        var storeModel = customerAttribute.MapTo<CustomerAttribute, CustomerAttributeStoreModel>();
        storeModel.IsGlobalAttribute = false;
        return View(storeModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(id);
        if (customerAttribute == null)
            return RedirectToAction("List");

        if (!customerAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("Edit", new { id });

        if (ModelState.IsValid)
        {
            await _customerAttributeViewModelService.DeleteCustomerAttribute(id);
            Success(_translationService.GetResource("Admin.Customers.CustomerAttributes.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    #endregion

    #region Customer attribute values

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ValueList(string customerAttributeId, DataSourceRequest command)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(customerAttributeId);
        if (customerAttribute == null || !IsVisibleToStore(customerAttribute))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var values = await _customerAttributeViewModelService.PrepareCustomerAttributeValues(customerAttributeId);
        var gridModel = new DataSourceResult {
            Data = values.ToList(),
            Total = values.Count()
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(string customerAttributeId)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(customerAttributeId);
        if (customerAttribute == null || !customerAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var model = _customerAttributeViewModelService.PrepareCustomerAttributeValueModel(customerAttributeId);
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(CustomerAttributeValueModel model)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(model.CustomerAttributeId);
        if (customerAttribute == null || !customerAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _customerAttributeViewModelService.InsertCustomerAttributeValueModel(model);
            return Content("");
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(string id, string customerAttributeId)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(customerAttributeId);
        if (customerAttribute == null || !customerAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var cav = customerAttribute.CustomerAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            return RedirectToAction("List");

        var model = _customerAttributeViewModelService.PrepareCustomerAttributeValueModel(cav);
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
        });

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(CustomerAttributeValueModel model)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(model.CustomerAttributeId);
        if (customerAttribute == null || !customerAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var cav = customerAttribute.CustomerAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _customerAttributeViewModelService.UpdateCustomerAttributeValueModel(model, cav);
            return Content("");
        }

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueDelete(CustomerAttributeValueModel model)
    {
        var customerAttribute = await _customerAttributeService.GetCustomerAttributeById(model.CustomerAttributeId);
        if (customerAttribute == null || !customerAttribute.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        if (ModelState.IsValid)
        {
            await _customerAttributeViewModelService.DeleteCustomerAttributeValue(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
