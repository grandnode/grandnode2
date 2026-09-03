using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Pages;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Pages;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Pages)]
[AutoValidateAntiforgeryToken]
public abstract class BasePageController(
    IPageViewModelService pageViewModelService,
    IPageService pageService,
    ILanguageService languageService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IAdminDataScope<Page> scope)
    : BaseController
{
    /// <summary>Hook for host-specific UI-copy warnings on Edit(GET) that aren't access-scope
    /// decisions. Overridden by the Store subclass (Task 3); no-op everywhere else. Third occurrence
    /// of this exact pattern in ARCH-001 (Category, Blog, now Page) - treat as proven, not novel.</summary>
    protected virtual void EditWarningCheck(Page page) { }

    // Exposed for host subclasses: primary-constructor parameters are not visible to derived classes
    // by name in C#.
    protected ITranslationService TranslationService => translationService;
    protected IAdminDataScope<Page> Scope => scope;

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List() => View();

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, PageListModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;

        var pageModels = (await pageService.GetAllPages(model.SearchStoreId, true))
            .Select(x => x.ToModel(dateTimeService))
            .ToList();

        if (!string.IsNullOrEmpty(model.Name))
            pageModels = pageModels.Where(x =>
                x.SystemName.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()) ||
                (x.Title != null && x.Title.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()))).ToList();

        // "Error during serialization or deserialization using the JSON JavaScriptSerializer. The
        // length of the string exceeds the value set on the maxJsonLength property."
        foreach (var page in pageModels) page.Body = "";
        var total = pageModels.Count;
        var pagedData = pageModels.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize).ToList();
        var gridModel = new DataSourceResult {
            Data = pagedData,
            Total = total
        };
        return Json(gridModel);
    }

    #endregion

    #region Create / Edit / Delete

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new PageModel { DisplayOrder = 1 };
        if (scope.DefaultStoreId is not null) model.Published = true;
        await pageViewModelService.PrepareLayoutsModel(model);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(PageModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var page = await pageViewModelService.InsertPageModel(model);
            Success(translationService.GetResource("Admin.Content.Pages.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = page.Id }) : RedirectToAction("List");
        }

        await pageViewModelService.PrepareLayoutsModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var page = await pageService.GetPageById(id);
        if (page == null) return RedirectToAction("List");

        EditWarningCheck(page);
        if (!await scope.CanView(page)) return RedirectToAction("List");

        var model = page.ToModel(dateTimeService);
        // Ruled during spec-writing: use Request.Scheme for both hosts (Admin's original hardcoded
        // "http" - a strict improvement, disclosed in the PR body, not a silent behavior change).
        model.Url = Url.RouteUrl("Page", new { SeName = page.SeName }, Request.Scheme);
        // True when the page is global or shared with more than one store - a store manager may copy
        // it into their own store instead of editing it directly. Always computed; Admin's view never
        // renders the Copy button region regardless of this value.
        model.ShowCopyButton = !page.LimitedToStores || page.Stores.Count > 1;
        await pageViewModelService.PrepareLayoutsModel(model);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Title = page.GetTranslation(x => x.Title, languageId, false);
            locale.Body = page.GetTranslation(x => x.Body, languageId, false);
            locale.MetaKeywords = page.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = page.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = page.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = page.GetSeName(languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(PageModel model, bool continueEditing)
    {
        var page = await pageService.GetPageById(model.Id);
        if (page == null) return RedirectToAction("List");
        if (!await scope.HasAccess(page)) return RedirectToAction("Edit", new { id = page.Id });

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null)
            {
                model.Stores = [scope.DefaultStoreId];
                // Store cannot change a page's customer-group ACL at all - its Edit form has no such
                // field, and its original controller always discarded whatever was posted here in
                // favor of the existing page's own value. Preserved exactly, not a bug.
                model.CustomerGroups = page.CustomerGroups.ToArray();
            }

            page = await pageViewModelService.UpdatePageModel(page, model);
            Success(translationService.GetResource("Admin.Content.Pages.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = page.Id });
            }
            return RedirectToAction("List");
        }

        model.Url = Url.RouteUrl("Page", new { SeName = page.SeName }, Request.Scheme);
        await pageViewModelService.PrepareLayoutsModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var page = await pageService.GetPageById(id);
        if (page == null) return RedirectToAction("List");
        // Page-specific: denial redirects to List, NOT Edit (unlike Blog/Category's own Delete) -
        // verified directly from Store's original source, not assumed from sibling entities.
        if (!await scope.HasAccess(page)) return RedirectToAction("List");

        await pageViewModelService.DeletePage(page);
        Success(translationService.GetResource("Admin.Content.Pages.Deleted"));
        return RedirectToAction("List");
    }

    #endregion
}
