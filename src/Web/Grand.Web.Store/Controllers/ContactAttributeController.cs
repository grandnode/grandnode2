using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Store.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.ContactAttributes)]
public class ContactAttributeController : BaseStoreController
{
    private readonly IContactAttributeViewModelService _contactAttributeViewModelService;
    private readonly IContactAttributeService _contactAttributeService;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public ContactAttributeController(
        IContactAttributeViewModelService contactAttributeViewModelService,
        IContactAttributeService contactAttributeService,
        ILanguageService languageService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _contactAttributeViewModelService = contactAttributeViewModelService;
        _contactAttributeService = contactAttributeService;
        _languageService = languageService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    private bool IsVisibleToStore(ContactAttribute attr) =>
        !attr.LimitedToStores || attr.Stores.Contains(CurrentStoreId);

    #region Contact attributes

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
        var contactAttributes = await _contactAttributeViewModelService.PrepareContactAttributeListModel();
        var storeId = CurrentStoreId;
        var gridModel = new DataSourceResult {
            Data = contactAttributes.Select(x => new {
                x.Id,
                x.Name,
                x.AttributeControlTypeName,
                x.IsRequired,
                x.DisplayOrder,
                IsReadOnly = !(x.Stores != null && x.Stores.Length == 1 && x.Stores.Contains(storeId))
            }).ToList(),
            Total = contactAttributes.Count()
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new ContactAttributeStoreModel();
        await AddLocales(_languageService, model.Locales);
        await _contactAttributeViewModelService.PrepareConditionAttributes(model, null);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(ContactAttributeStoreModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            model.Stores = [CurrentStoreId];
            var contactAttribute = await _contactAttributeViewModelService.InsertContactAttributeModel(model);
            Success(_translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = contactAttribute.Id })
                : RedirectToAction("List");
        }

        await _contactAttributeViewModelService.PrepareConditionAttributes(model, null);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(id);
        if (contactAttribute == null || !IsVisibleToStore(contactAttribute))
            return RedirectToAction("List");

        var model = contactAttribute.MapTo<ContactAttribute, ContactAttributeStoreModel>();
        model.IsReadOnly = !contactAttribute.AccessToEntityByStore(CurrentStoreId);
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = contactAttribute.GetTranslation(x => x.Name, languageId, false);
            locale.TextPrompt = contactAttribute.GetTranslation(x => x.TextPrompt, languageId, false);
        });
        await _contactAttributeViewModelService.PrepareConditionAttributes(model, contactAttribute);

        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(ContactAttributeModel model, bool continueEditing)
    {
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(model.Id);
        if (contactAttribute == null)
            return RedirectToAction("List");

        if (!contactAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            model.Stores = [CurrentStoreId];
            contactAttribute = await _contactAttributeViewModelService.UpdateContactAttributeModel(contactAttribute, model);
            Success(_translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = contactAttribute.Id });
            }

            return RedirectToAction("List");
        }

        var storeModel = contactAttribute.MapTo<ContactAttribute, ContactAttributeStoreModel>();
        storeModel.IsReadOnly = false;
        await _contactAttributeViewModelService.PrepareConditionAttributes(storeModel, contactAttribute);
        return View(storeModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(id);
        if (contactAttribute == null)
            return RedirectToAction("List");

        if (!contactAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("Edit", new { id });

        if (ModelState.IsValid)
        {
            await _contactAttributeService.DeleteContactAttribute(contactAttribute);
            Success(_translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Deleted"));
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
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(contactAttributeId);
        if (contactAttribute == null || !IsVisibleToStore(contactAttribute))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var values = contactAttribute.ContactAttributeValues;
        var gridModel = new DataSourceResult {
            Data = values.Select(x => new ContactAttributeValueModel {
                Id = x.Id,
                ContactAttributeId = x.ContactAttributeId,
                Name = contactAttribute.AttributeControlType != AttributeControlType.ColorSquares
                    ? x.Name
                    : $"{x.Name} - {x.ColorSquaresRgb}",
                ColorSquaresRgb = x.ColorSquaresRgb,
                IsPreSelected = x.IsPreSelected,
                DisplayOrder = x.DisplayOrder
            }),
            Total = values.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(string contactAttributeId)
    {
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(contactAttributeId);
        if (contactAttribute == null || !contactAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var model = _contactAttributeViewModelService.PrepareContactAttributeValueModel(contactAttribute);
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueCreatePopup(ContactAttributeValueModel model)
    {
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(model.ContactAttributeId);
        if (contactAttribute == null || !contactAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _contactAttributeViewModelService.InsertContactAttributeValueModel(contactAttribute, model);
            return Content("");
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(string id, string contactAttributeId)
    {
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(contactAttributeId);
        if (contactAttribute == null || !contactAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var cav = contactAttribute.ContactAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            return RedirectToAction("List");

        var model = _contactAttributeViewModelService.PrepareContactAttributeValueModel(contactAttribute, cav);
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
        });

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueEditPopup(ContactAttributeValueModel model)
    {
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(model.ContactAttributeId);
        if (contactAttribute == null || !contactAttribute.AccessToEntityByStore(CurrentStoreId))
            return RedirectToAction("List");

        var cav = contactAttribute.ContactAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await _contactAttributeViewModelService.UpdateContactAttributeValueModel(contactAttribute, cav, model);
            return Content("");
        }

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ValueDelete(string id, string contactAttributeId)
    {
        var contactAttribute = await _contactAttributeService.GetContactAttributeById(contactAttributeId);
        if (contactAttribute == null || !contactAttribute.AccessToEntityByStore(CurrentStoreId))
            return new JsonResult(new DataSourceResult { Errors = "Access denied" });

        var cav = contactAttribute.ContactAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            throw new ArgumentException("No contact attribute value found with the specified id");

        if (ModelState.IsValid)
        {
            contactAttribute.ContactAttributeValues.Remove(cav);
            await _contactAttributeService.UpdateContactAttribute(contactAttribute);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
