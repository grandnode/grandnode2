using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Languages)]
public class LanguageController(
    ILanguageService languageService,
    ITranslationService translationService,
    IStoreService storeService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> ListData()
    {
        var storeId = CurrentStoreId;

        var store = await storeService.GetStoreById(storeId);
        var defaultLanguageId = store?.DefaultLanguageId;

        var languages = await languageService.GetAllLanguages(showHidden: false);

        var items = languages
            .Select(l => new StoreLanguageModel {
                Id = l.Id,
                Name = l.Name,
                LanguageCulture = l.LanguageCulture,
                FlagImageFileName = l.FlagImageFileName,
                Published = l.Published,
                DisplayOrder = l.DisplayOrder,
                LimitedToStores = l.LimitedToStores,
                IsAssignedToCurrentStore = !l.LimitedToStores || l.Stores.Contains(storeId),
                IsDefaultStoreLanguage = l.Id == defaultLanguageId,
                CanManage = l.LimitedToStores
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AssignStore(string id)
    {
        var language = await languageService.GetLanguageById(id);
        if (language == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.NotFound") });

        if (!language.LimitedToStores)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.CannotModifyGlobal") });

        var storeId = CurrentStoreId;
        if (!language.Stores.Contains(storeId))
        {
            language.Stores.Add(storeId);
            await languageService.UpdateLanguage(language);
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UnassignStore(string id)
    {
        var language = await languageService.GetLanguageById(id);
        if (language == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.NotFound") });

        if (!language.LimitedToStores)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.CannotModifyGlobal") });

        var storeId = CurrentStoreId;

        var store = await storeService.GetStoreById(storeId);
        if (store?.DefaultLanguageId == language.Id)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.CantUnassignDefault") });

        if (language.Stores.Remove(storeId))
            await languageService.UpdateLanguage(language);

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SetDefaultLanguage(string id)
    {
        var language = await languageService.GetLanguageById(id);
        if (language == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.NotFound") });

        if (!language.Published)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.NotPublished") });

        var storeId = CurrentStoreId;

        if (language.LimitedToStores && !language.Stores.Contains(storeId))
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.NotAssignedToStore") });

        var store = await storeService.GetStoreById(storeId);
        if (store == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Stores.NotFound") });

        store.DefaultLanguageId = language.Id;
        await storeService.UpdateStore(store);

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UnsetDefaultLanguage(string id)
    {
        var language = await languageService.GetLanguageById(id);
        if (language == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.NotFound") });

        var storeId = CurrentStoreId;
        var store = await storeService.GetStoreById(storeId);
        if (store == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Stores.NotFound") });

        if (store.DefaultLanguageId != language.Id)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Languages.NotDefaultLanguage") });

        store.DefaultLanguageId = string.Empty;
        await storeService.UpdateStore(store);

        return Json(new { success = true });
    }
}
