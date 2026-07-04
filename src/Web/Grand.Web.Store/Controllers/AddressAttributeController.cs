using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Store.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.AddressAttributes)]
public class AddressAttributeController : BaseStoreController
{
    private readonly IAddressAttributeViewModelService _addressAttributeViewModelService;
    private readonly IAddressAttributeService _addressAttributeService;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public AddressAttributeController(
        IAddressAttributeViewModelService addressAttributeViewModelService,
        IAddressAttributeService addressAttributeService,
        ILanguageService languageService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _addressAttributeViewModelService = addressAttributeViewModelService;
        _addressAttributeService = addressAttributeService;
        _languageService = languageService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    private bool IsVisibleToStore(AddressAttribute attr) =>
        !attr.LimitedToStores || attr.Stores.Contains(CurrentStoreId);

    #region Address attributes

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
        var (addressAttributes, _) = await _addressAttributeViewModelService.PrepareAddressAttributes();
        var visibleAttributes = addressAttributes
            .Where(x => x.Stores == null || x.Stores.Length == 0 || x.Stores.Contains(storeId))
            .ToList();
        var gridModel = new DataSourceResult {
            Data = visibleAttributes.Select(x => new {
                x.Id,
                x.Name,
                x.AttributeControlTypeName,
                x.IsRequired,
                x.DisplayOrder,
                IsGlobalAttribute = !(x.Stores is { Length: 1 } && x.Stores.Contains(storeId))
            }).ToList(),
            Total = visibleAttributes.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new AddressAttributeStoreModel();
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(AddressAttributeStoreModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            model.Stores = [CurrentStoreId];
            var addressAttribute = await _addressAttributeViewModelService.InsertAddressAttributeModel(model);
            Success(_translationService.GetResource("Admin.Address.AddressAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = addressAttribute.Id })
                : RedirectToAction("List");
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(id);
        if (addressAttribute == null || !IsVisibleToStore(addressAttribute))
            return RedirectToAction("List");

        var model = addressAttribute.MapTo<AddressAttribute, AddressAttributeStoreModel>();
        model.IsGlobalAttribute = !addressAttribute.AccessToEntityByStore(CurrentStoreId);
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = addressAttribute.GetTranslation(x => x.Name, languageId, false);
        });

        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(AddressAttributeModel model, bool continueEditing)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(model.Id);
        if (addressAttribute == null)
            return RedirectToAction("List");

        if (!addressAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            model.Stores = [CurrentStoreId];
            addressAttribute =
                await _addressAttributeViewModelService.UpdateAddressAttributeModel(model, addressAttribute);
            Success(_translationService.GetResource("Admin.Address.AddressAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = addressAttribute.Id });
            }

            return RedirectToAction("List");
        }

        var storeModel = addressAttribute.MapTo<AddressAttribute, AddressAttributeStoreModel>();
        storeModel.IsGlobalAttribute = false;
        await AddLocales(_languageService, storeModel.Locales, (locale, languageId) =>
        {
            locale.Name = addressAttribute.GetTranslation(x => x.Name, languageId, false);
        });
        return View(storeModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(id);
        if (addressAttribute == null)
            return RedirectToAction("List");

        if (!addressAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("Edit", new { id });

        if (ModelState.IsValid)
        {
            await _addressAttributeService.DeleteAddressAttribute(addressAttribute);
            Success(_translationService.GetResource("Admin.Address.AddressAttributes.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    #endregion

    #region Address attribute values

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ValueList(string addressAttributeId, DataSourceRequest command)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(addressAttributeId);
        if (addressAttribute == null || !IsVisibleToStore(addressAttribute))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var (addressAttributeValues, totalCount) =
            await _addressAttributeViewModelService.PrepareAddressAttributeValues(addressAttributeId);
        var gridModel = new DataSourceResult {
            Data = addressAttributeValues.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(string addressAttributeId)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(addressAttributeId);
        if (addressAttribute == null || !addressAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var model = _addressAttributeViewModelService.PrepareAddressAttributeValueModel(addressAttributeId);
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(AddressAttributeValueModel model)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
        if (addressAttribute == null || !addressAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _addressAttributeViewModelService.InsertAddressAttributeValueModel(model);
            return Content("");
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(string id, string addressAttributeId)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(addressAttributeId);
        if (addressAttribute == null || !addressAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var cav = addressAttribute.AddressAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            return RedirectToAction("List");

        var model = _addressAttributeViewModelService.PrepareAddressAttributeValueModel(cav);
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
        });

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(AddressAttributeValueModel model)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
        if (addressAttribute == null || !addressAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var cav = addressAttribute.AddressAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _addressAttributeViewModelService.UpdateAddressAttributeValueModel(model, cav);
            return Content("");
        }

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueDelete(AddressAttributeValueModel model)
    {
        var addressAttribute = await _addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
        if (addressAttribute == null || !addressAttribute.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var cav = addressAttribute.AddressAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return new JsonResult(new DataSourceResult
                { Errors = "No address attribute value found with the specified id" });

        if (ModelState.IsValid)
        {
            await _addressAttributeService.DeleteAddressAttributeValue(cav);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
