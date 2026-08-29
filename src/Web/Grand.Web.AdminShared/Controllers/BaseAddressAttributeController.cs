using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.AddressAttributes)]
public abstract class BaseAddressAttributeController(
    IAddressAttributeService addressAttributeService,
    IAddressAttributeViewModelService addressAttributeViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<AddressAttribute> scope) : BaseController
{
    #region Address attributes

    public virtual IActionResult Index() => RedirectToAction("List");

    public virtual IActionResult List() => View();

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public virtual async Task<IActionResult> List(DataSourceRequest command)
    {
        var (addressAttributes, _) = await addressAttributeViewModelService.PrepareAddressAttributes();
        var storeId = scope.DefaultStoreId;
        var visible = storeId is null
            ? addressAttributes.ToList()
            : addressAttributes
                .Where(x => x.Stores == null || x.Stores.Length == 0 || x.Stores.Contains(storeId))
                .ToList();
        var gridModel = new DataSourceResult {
            Data = visible.Select(x => new {
                x.Id, x.Name, x.AttributeControlTypeName, x.IsRequired, x.DisplayOrder,
                IsGlobalAttribute = storeId is not null &&
                    !(x.Stores is { Length: 1 } && x.Stores.Contains(storeId))
            }).ToList(),
            Total = visible.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public virtual async Task<IActionResult> Create()
    {
        var model = addressAttributeViewModelService.PrepareAddressAttributeModel();
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public virtual async Task<IActionResult> Create(AddressAttributeModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var addressAttribute = await addressAttributeViewModelService.InsertAddressAttributeModel(model);
            Success(translationService.GetResource("Admin.Address.AddressAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = addressAttribute.Id })
                : RedirectToAction("List");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public virtual async Task<IActionResult> Edit(string id)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(id);
        if (addressAttribute == null || !await scope.CanView(addressAttribute))
            return RedirectToAction("List");

        var model = addressAttributeViewModelService.PrepareAddressAttributeModel(addressAttribute);
        model.IsGlobalAttribute = scope.DefaultStoreId is not null &&
            !await scope.HasAccess(addressAttribute);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = addressAttribute.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public virtual async Task<IActionResult> Edit(AddressAttributeModel model, bool continueEditing)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(model.Id);
        if (addressAttribute == null || !await scope.HasAccess(addressAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            addressAttribute = await addressAttributeViewModelService.UpdateAddressAttributeModel(model, addressAttribute);
            Success(translationService.GetResource("Admin.Address.AddressAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = addressAttribute.Id });
            }
            return RedirectToAction("List");
        }

        model.IsGlobalAttribute = false;
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = addressAttribute.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public virtual async Task<IActionResult> Delete(string id)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(id);
        if (addressAttribute == null)
            return RedirectToAction("List");
        if (!await scope.HasAccess(addressAttribute))
            return RedirectToAction("Edit", new { id });

        await addressAttributeService.DeleteAddressAttribute(addressAttribute);
        Success(translationService.GetResource("Admin.Address.AddressAttributes.Deleted"));
        return RedirectToAction("List");
    }

    #endregion

    #region Address attribute values

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public virtual async Task<IActionResult> ValueList(string addressAttributeId, DataSourceRequest command)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(addressAttributeId);
        if (addressAttribute == null || !await scope.CanView(addressAttribute))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var (values, total) = await addressAttributeViewModelService.PrepareAddressAttributeValues(addressAttributeId);
        return Json(new DataSourceResult { Data = values.ToList(), Total = total });
    }

    // Note: ValueCreatePopup (both GET and POST) intentionally undecorated here as the permission
    // action name diverges between Admin (Create) and Store (Edit). Each subclass overrides to apply
    // its own [PermissionAuthorizeAction] attribute.
    public virtual async Task<IActionResult> ValueCreatePopup(string addressAttributeId)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(addressAttributeId);
        if (addressAttribute == null || !await scope.HasAccess(addressAttribute))
            return RedirectToAction("List");

        var model = addressAttributeViewModelService.PrepareAddressAttributeValueModel(addressAttributeId);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> ValueCreatePopup(AddressAttributeValueModel model)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
        if (addressAttribute == null || !await scope.HasAccess(addressAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await addressAttributeViewModelService.InsertAddressAttributeValueModel(model);
            return Content("");
        }
        return View(model);
    }

    // Note: ValueEditPopup GET intentionally undecorated here as the permission action name diverges
    // between Admin (Preview) and Store (Edit). Each subclass overrides to apply its own
    // [PermissionAuthorizeAction] attribute.
    public virtual async Task<IActionResult> ValueEditPopup(string id, string addressAttributeId)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(addressAttributeId);
        if (addressAttribute == null || !await scope.HasAccess(addressAttribute))
            return RedirectToAction("List");

        var cav = addressAttribute.AddressAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            return RedirectToAction("List");

        var model = addressAttributeViewModelService.PrepareAddressAttributeValueModel(cav);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public virtual async Task<IActionResult> ValueEditPopup(AddressAttributeValueModel model)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
        if (addressAttribute == null || !await scope.HasAccess(addressAttribute))
            return RedirectToAction("List");

        var cav = addressAttribute.AddressAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await addressAttributeViewModelService.UpdateAddressAttributeValueModel(model, cav);
            return Content("");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public virtual async Task<IActionResult> ValueDelete(AddressAttributeValueModel model)
    {
        var addressAttribute = await addressAttributeService.GetAddressAttributeById(model.AddressAttributeId);
        if (addressAttribute == null || !await scope.HasAccess(addressAttribute))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var cav = addressAttribute.AddressAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return new JsonResult(new DataSourceResult
                { Errors = "No address attribute value found with the specified id" });

        if (ModelState.IsValid)
        {
            await addressAttributeService.DeleteAddressAttributeValue(cav);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
