using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Pages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Pages)]
public class PageController : BaseStoreController
{
    #region Constructors

    public PageController(
        IPageViewModelService pageViewModelService,
        IPageService pageService,
        ILanguageService languageService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IDateTimeService dateTimeService)
    {
        _pageViewModelService = pageViewModelService;
        _pageService = pageService;
        _languageService = languageService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
        _dateTimeService = dateTimeService;
    }

    #endregion

    #region Fields

    private readonly IPageViewModelService _pageViewModelService;
    private readonly IPageService _pageService;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IDateTimeService _dateTimeService;

    #endregion

    #region List

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        return View();
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> StorePagesList(DataSourceRequest command, PageListModel model)
    {
        var storeId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        var pages = await _pageService.GetAllPages(storeId, true);

        // Store-specific: exclusively assigned to this one store
        var pageModels = pages
            .Where(x => x.LimitedToStores && x.Stores.Count == 1)
            .Select(x => x.ToModel(_dateTimeService))
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
        var storeId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        var pages = await _pageService.GetAllPages(storeId, true);

        // Global: no store restriction, or shared across multiple stores
        var pageModels = pages
            .Where(x => !x.LimitedToStores || x.Stores.Count > 1)
            .Select(x => x.ToModel(_dateTimeService))
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

    #endregion

    #region Create / Edit / Delete

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        var model = new PageModel {
            DisplayOrder = 1,
            Published = true
        };
        await _pageViewModelService.PrepareLayoutsModel(model);
        await AddLocales(_languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(PageModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
            var page = await _pageViewModelService.InsertPageModel(model);
            Success(_translationService.GetResource("Admin.Content.Pages.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = page.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        await _pageViewModelService.PrepareLayoutsModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var page = await _pageService.GetPageById(id);
        if (page == null)
            return RedirectToAction("List");

        if (!page.LimitedToStores || (page.Stores.Contains(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) &&
                                      page.Stores.Count > 1))
        {
            Warning(_translationService.GetResource("Admin.Content.Pages.Permissions"));
        }
        else
        {
            if (!page.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                return RedirectToAction("List");
        }

        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        ViewBag.ShowCopyButton = !page.LimitedToStores || page.Stores.Count > 1;
        var model = page.ToModel(_dateTimeService);
        model.Url = Url.RouteUrl("Page", new { SeName = page.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id) }, Request.Scheme);
        await _pageViewModelService.PrepareLayoutsModel(model);
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
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
        var page = await _pageService.GetPageById(model.Id);
        if (page == null)
            return RedirectToAction("List");

        if (!page.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("Edit", new { id = page.Id });

        if (ModelState.IsValid)
        {
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
            model.CustomerGroups = page.CustomerGroups.ToArray();
            page = await _pageViewModelService.UpdatePageModel(page, model);
            Success(_translationService.GetResource("Admin.Content.Pages.Updated"));

            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = page.Id });
            }

            return RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);
        model.Url = Url.RouteUrl("Page", new { SeName = page.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id) }, "http");
        await _pageViewModelService.PrepareLayoutsModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var page = await _pageService.GetPageById(id);
        if (page == null)
            return RedirectToAction("List");

        if (!page.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("List");

        await _pageViewModelService.DeletePage(page);
        Success(_translationService.GetResource("Admin.Content.Pages.Deleted"));
        return RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> Copy(string id)
    {
        var storeId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        var page = await _pageService.GetPageById(id);
        if (page == null)
            return RedirectToAction("List");

        if (!page.AccessToEntityByStore(storeId))
            return RedirectToAction("List");

        // Only allow copy for multistore or store-unrestricted topics
        if (page.LimitedToStores && page.Stores.Count <= 1)
            return RedirectToAction("Edit", new { id });

        // Check if a page with the same SystemName already exists for the current store
        var storePages = await _pageService.GetAllPages(storeId, true);
        if (storePages.Any(p => p.Id != page.Id &&
                                p.SystemName.Equals(page.SystemName, StringComparison.OrdinalIgnoreCase)))
        {
            Error(_translationService.GetResource("Admin.Content.Pages.Copy.DuplicateSystemName"));
            return RedirectToAction("Edit", new { id });
        }

        // Build copy model from original page
        var model = page.ToModel(_dateTimeService);
        model.Id = "";
        model.Stores = [storeId];

        // Preserve localized content
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Title = page.GetTranslation(x => x.Title, languageId, false);
            locale.Body = page.GetTranslation(x => x.Body, languageId, false);
            locale.MetaKeywords = page.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = page.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = page.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = page.GetSeName(languageId, false);
        });

        var newPage = await _pageViewModelService.InsertPageModel(model);
        Success(_translationService.GetResource("Admin.Content.Pages.Added"));
        return RedirectToAction("Edit", new { id = newPage.Id });
    }

    #endregion
}
