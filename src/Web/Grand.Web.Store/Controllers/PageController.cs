using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Pages;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Pages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BasePageController (ARCH-001 Page consolidation). All shared
// behavior lives in the base; this class supplies Store's DI wiring, the EditWarningCheck hook, and
// three genuinely Store-only actions (Copy, StorePagesList, GlobalPagesList) that have no Admin
// equivalent and are not shared - Admin is already global, "copy into my store" and the two-tab
// list split are Store-specific UI/workflow, not security-scope differences. Same pattern as
// BlogController's kept Preview action.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class PageController(
    IPageViewModelService pageViewModelService,
    IPageService pageService,
    ILanguageService languageService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IAdminDataScope<Page> scope)
    : BasePageController(pageViewModelService, pageService, languageService, translationService,
        dateTimeService, scope)
{
    // Re-derived from the original Store PageController.Edit(GET) - the condition is unusual (warns
    // when NOT limited to stores at all, or when limited AND the staff member's store is one of
    // several) and easy to get backwards. Third occurrence of this exact idiom in ARCH-001
    // (Category, Blog, now Page) - treat as proven.
    protected override void EditWarningCheck(Page page)
    {
        if (!page.LimitedToStores ||
            (page.LimitedToStores &&
             page.Stores.Contains(Scope.DefaultStoreId) &&
             page.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Content.Pages.Permissions"));
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> StorePagesList(DataSourceRequest command, PageListModel model)
    {
        var pages = await pageService.GetAllPages(Scope.DefaultStoreId, true);

        var pageModels = pages
            .Where(x => x.LimitedToStores && x.Stores.Count == 1)
            .Select(x => x.ToModel(dateTimeService))
            .ToList();

        if (!string.IsNullOrEmpty(model.Name))
            pageModels = pageModels.Where(x =>
                x.SystemName.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()) ||
                (x.Title != null && x.Title.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()))).ToList();

        foreach (var page in pageModels) page.Body = "";

        var total = pageModels.Count;
        var pagedData = pageModels.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize).ToList();
        return Json(new DataSourceResult { Data = pagedData, Total = total });
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> GlobalPagesList(DataSourceRequest command, PageListModel model)
    {
        var pages = await pageService.GetAllPages(Scope.DefaultStoreId, true);

        var pageModels = pages
            .Where(x => !x.LimitedToStores || x.Stores.Count > 1)
            .Select(x => x.ToModel(dateTimeService))
            .ToList();

        if (!string.IsNullOrEmpty(model.Name))
            pageModels = pageModels.Where(x =>
                x.SystemName.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()) ||
                (x.Title != null && x.Title.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()))).ToList();

        foreach (var page in pageModels) page.Body = "";

        var total = pageModels.Count;
        var pagedData = pageModels.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize).ToList();
        return Json(new DataSourceResult { Data = pagedData, Total = total });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> Copy(string id)
    {
        var storeId = Scope.DefaultStoreId;
        var page = await pageService.GetPageById(id);
        if (page == null) return RedirectToAction("List");

        // A page is copyable only while it is still readable here and not yet owned by this store, so
        // HasAccess - which demands sole ownership - cannot be the guard.
        if (page.LimitedToStores && !page.Stores.Contains(storeId))
            return RedirectToAction("List");

        // Only allow copy for multistore or store-unrestricted pages.
        if (page.LimitedToStores && page.Stores.Count <= 1)
            return RedirectToAction("Edit", new { id });

        var storePages = await pageService.GetAllPages(storeId, true);
        if (storePages.Any(p => p.Id != page.Id &&
                                 p.SystemName.Equals(page.SystemName, StringComparison.OrdinalIgnoreCase)))
        {
            Error(translationService.GetResource("Admin.Content.Pages.Copy.DuplicateSystemName"));
            return RedirectToAction("Edit", new { id });
        }

        var model = page.ToModel(dateTimeService);
        model.Id = "";
        model.Stores = [storeId];

        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Title = page.GetTranslation(x => x.Title, languageId, false);
            locale.Body = page.GetTranslation(x => x.Body, languageId, false);
            locale.MetaKeywords = page.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = page.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = page.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = page.GetSeName(languageId, false);
        });

        var newPage = await pageViewModelService.InsertPageModel(model);
        Success(translationService.GetResource("Admin.Content.Pages.Added"));
        return RedirectToAction("Edit", new { id = newPage.Id });
    }
}
