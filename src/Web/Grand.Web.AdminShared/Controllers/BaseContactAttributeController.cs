using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.ContactAttributes)]
public abstract class BaseContactAttributeController(
    IContactAttributeViewModelService contactAttributeViewModelService,
    IContactAttributeService contactAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IAdminDataScope<ContactAttribute> scope) : BaseController
{
    #region Contact attributes

    public IActionResult Index() => RedirectToAction("List");
    public IActionResult List() => View();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var contactAttributes = await contactAttributeViewModelService.PrepareContactAttributeListModel();
        var storeId = scope.DefaultStoreId;
        var visible = storeId is null
            ? contactAttributes.ToList()
            : contactAttributes.Where(x => x.Stores == null || x.Stores.Length == 0 || x.Stores.Contains(storeId)).ToList();
        var gridModel = new DataSourceResult {
            Data = visible.Select(x => new {
                x.Id, x.Name, x.AttributeControlTypeName, x.IsRequired, x.DisplayOrder,
                IsReadOnly = storeId is not null && !(x.Stores is { Length: 1 } && x.Stores.Contains(storeId))
            }).ToList(),
            Total = visible.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new ContactAttributeModel();
        await AddLocales(languageService, model.Locales);
        await contactAttributeViewModelService.PrepareConditionAttributes(model, null);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(ContactAttributeModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var contactAttribute = await contactAttributeViewModelService.InsertContactAttributeModel(model);
            Success(translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = contactAttribute.Id })
                : RedirectToAction("List");
        }
        await contactAttributeViewModelService.PrepareConditionAttributes(model, null);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(id);
        if (contactAttribute == null || !await scope.CanView(contactAttribute))
            return RedirectToAction("List");

        var model = contactAttribute.ToModel();
        model.IsReadOnly = scope.DefaultStoreId is not null && !await scope.HasAccess(contactAttribute);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = contactAttribute.GetTranslation(x => x.Name, languageId, false);
            locale.TextPrompt = contactAttribute.GetTranslation(x => x.TextPrompt, languageId, false);
        });
        await contactAttributeViewModelService.PrepareConditionAttributes(model, contactAttribute);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(ContactAttributeModel model, bool continueEditing)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(model.Id);
        if (contactAttribute == null || !await scope.HasAccess(contactAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            contactAttribute = await contactAttributeViewModelService.UpdateContactAttributeModel(contactAttribute, model);
            Success(translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = contactAttribute.Id });
            }
            return RedirectToAction("List");
        }

        model.IsReadOnly = false;
        await contactAttributeViewModelService.PrepareConditionAttributes(model, contactAttribute);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(id);
        if (contactAttribute == null)
            return RedirectToAction("List");
        if (!await scope.HasAccess(contactAttribute))
            return RedirectToAction("Edit", new { id });

        if (ModelState.IsValid)
        {
            await contactAttributeService.DeleteContactAttribute(contactAttribute);
            Success(translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Deleted"));
            return RedirectToAction("List");
        }
        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    #endregion

    #region Contact attribute values

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ValueList(string contactAttributeId, DataSourceRequest command)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(contactAttributeId);
        if (contactAttribute == null || !await scope.CanView(contactAttribute))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var values = contactAttribute.ContactAttributeValues;
        var gridModel = new DataSourceResult {
            Data = values.Select(x => new ContactAttributeValueModel {
                Id = x.Id, ContactAttributeId = x.ContactAttributeId,
                Name = contactAttribute.AttributeControlType != AttributeControlType.ColorSquares
                    ? x.Name : $"{x.Name} - {x.ColorSquaresRgb}",
                ColorSquaresRgb = x.ColorSquaresRgb, IsPreSelected = x.IsPreSelected, DisplayOrder = x.DisplayOrder
            }),
            Total = values.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(string contactAttributeId)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(contactAttributeId);
        if (contactAttribute == null || !await scope.HasAccess(contactAttribute))
            return RedirectToAction("List");

        var model = contactAttributeViewModelService.PrepareContactAttributeValueModel(contactAttribute);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(ContactAttributeValueModel model)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(model.ContactAttributeId);
        if (contactAttribute == null || !await scope.HasAccess(contactAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await contactAttributeViewModelService.InsertContactAttributeValueModel(contactAttribute, model);
            return Content("");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(string id, string contactAttributeId)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(contactAttributeId);
        if (contactAttribute == null || !await scope.HasAccess(contactAttribute))
            return RedirectToAction("List");

        var cav = contactAttribute.ContactAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            return RedirectToAction("List");

        var model = contactAttributeViewModelService.PrepareContactAttributeValueModel(contactAttribute, cav);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(ContactAttributeValueModel model)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(model.ContactAttributeId);
        if (contactAttribute == null || !await scope.HasAccess(contactAttribute))
            return RedirectToAction("List");

        var cav = contactAttribute.ContactAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await contactAttributeViewModelService.UpdateContactAttributeValueModel(contactAttribute, cav, model);
            return Content("");
        }
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueDelete(string id, string contactAttributeId)
    {
        var contactAttribute = await contactAttributeService.GetContactAttributeById(contactAttributeId);
        if (contactAttribute == null || !await scope.HasAccess(contactAttribute))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var cav = contactAttribute.ContactAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            throw new ArgumentException("No contact attribute value found with the specified id");

        if (ModelState.IsValid)
        {
            contactAttribute.ContactAttributeValues.Remove(cav);
            await contactAttributeService.UpdateContactAttribute(contactAttribute);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
