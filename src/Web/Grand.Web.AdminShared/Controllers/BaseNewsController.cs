using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.News;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.News;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.News)]
[AutoValidateAntiforgeryToken]
public abstract class BaseNewsController(
    INewsViewModelService newsViewModelService,
    INewsService newsService,
    ILanguageService languageService,
    ITranslationService translationService,
    IStoreService storeService,
    IDateTimeService dateTimeService,
    IAdminDataScope<NewsItem> scope)
    : BaseController
{
    /// <summary>Hook for host-specific UI-copy warnings on Edit(GET) that aren't access-scope
    /// decisions. Overridden by the Store subclass (Task 4); no-op everywhere else. Fourth occurrence
    /// of this exact pattern in ARCH-001 (Category, Blog, Page, now News) - treat as proven.</summary>
    protected virtual void EditWarningCheck(NewsItem newsItem) { }

    // Exposed for host subclasses: primary-constructor parameters are not visible to derived classes
    // by name in C#.
    protected ITranslationService TranslationService => translationService;
    protected IAdminDataScope<NewsItem> Scope => scope;

    #region News items

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        var model = new NewsItemListModel();
        if (scope.DefaultStoreId is null)
        {
            model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, NewsItemListModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;

        var news = await newsViewModelService.PrepareNewsItemModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = news.newsItemModels.ToList(),
            Total = news.totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        var model = new NewsItemModel {
            Published = true,
            AllowComments = true
        };
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(NewsItemModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            // Store's original called _newsService.UpdateNews(newsItem) again here, right after
            // InsertNewsItemModel (which already inserts internally) - a redundant double-write with
            // no purpose. Dropped as a ruled, disclosed fix.
            var newsItem = await newsViewModelService.InsertNewsItemModel(model);
            Success(translationService.GetResource("Admin.Content.News.NewsItems.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = newsItem.Id }) : RedirectToAction("List");
        }

        // Ruled fix: Admin's original assigned the unawaited Task here instead of the resolved list.
        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var newsItem = await newsService.GetNewsById(id);
        if (newsItem == null) return RedirectToAction("List");

        EditWarningCheck(newsItem);
        if (!await scope.CanView(newsItem)) return RedirectToAction("List");

        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        var model = newsItem.ToModel(dateTimeService);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Title = newsItem.GetTranslation(x => x.Title, languageId, false);
            locale.Short = newsItem.GetTranslation(x => x.Short, languageId, false);
            locale.Full = newsItem.GetTranslation(x => x.Full, languageId, false);
            locale.MetaKeywords = newsItem.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = newsItem.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = newsItem.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = newsItem.GetSeName(languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(NewsItemModel model, bool continueEditing)
    {
        var newsItem = await newsService.GetNewsById(model.Id);
        if (newsItem == null) return RedirectToAction("List");
        if (!await scope.HasAccess(newsItem)) return RedirectToAction("Edit", new { id = newsItem.Id });

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            newsItem = await newsViewModelService.UpdateNewsItemModel(newsItem, model);
            Success(translationService.GetResource("Admin.Content.News.NewsItems.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = newsItem.Id });
            }
            return RedirectToAction("List");
        }

        ViewBag.AllLanguages = await languageService.GetAllLanguages(true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var newsItem = await newsService.GetNewsById(id);
        if (newsItem == null) return RedirectToAction("List");
        // News-specific: denial redirects to List, NOT Edit (matches Page's precedent, not
        // Blog/Category's own Delete target) - verified directly from Store's original source.
        if (!await scope.HasAccess(newsItem)) return RedirectToAction("List");

        await newsService.DeleteNews(newsItem);
        Success(translationService.GetResource("Admin.Content.News.NewsItems.Deleted"));
        return RedirectToAction("List");
    }

    #endregion

    #region Comments

    public IActionResult Comments(string filterByNewsItemId)
    {
        ViewBag.FilterByNewsItemId = filterByNewsItemId;
        return View();
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> Comments(string filterByNewsItemId, DataSourceRequest command)
    {
        var comments = await newsViewModelService.PrepareNewsCommentModel(filterByNewsItemId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = comments.newsCommentModels.ToList(),
            Total = comments.totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> CommentDelete(NewsComment model)
    {
        var newsItem = await newsService.GetNewsById(model.NewsItemId);
        // Admin's original had no scope check on CommentDelete at all - but Comments/CommentDelete are
        // newly shared with Store in this phase (fixing a live bug where Store's Edit.cshtml already
        // rendered a Comments tab whose grid called actions that didn't exist), so this check is
        // required for Store's benefit even though Admin's original never needed one (GlobalAdminDataScope
        // makes it a no-op for Admin). Same pattern Blog's own CommentDelete already uses.
        if (newsItem == null || !await scope.HasAccess(newsItem))
            return ErrorForKendoGridJson("No access to this news item's comments");

        if (ModelState.IsValid)
        {
            await newsViewModelService.CommentDelete(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
