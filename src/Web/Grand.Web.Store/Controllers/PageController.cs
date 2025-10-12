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

    #endregion Fields

    #region List

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List()
    {
        var model = await _pageViewModelService.PreparePageListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, PageListModel model)
    {
        var storeId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        var allPages = await _pageService.GetAllPages("", true);
        
        // Filter to show only pages for this store or global pages
        var filteredPages = allPages
            .Where(x => !x.LimitedToStores || x.Stores.Contains(storeId))
            .ToList();

        // Group by SystemName and prefer store-specific over global
        var groupedPages = filteredPages
            .GroupBy(x => x.SystemName)
            .Select(g =>
            {
                // If there's a store-specific version, use it; otherwise use global
                var storeSpecific = g.FirstOrDefault(p => p.LimitedToStores && p.Stores.Contains(storeId));
                return storeSpecific ?? g.First();
            })
            .Select(x => x.ToModel(_dateTimeService))
            .ToList();

        if (!string.IsNullOrEmpty(model.Name))
            groupedPages = groupedPages.Where
            (x => x.SystemName.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()) ||
                  (x.Title != null && x.Title.ToLowerInvariant().Contains(model.Name.ToLowerInvariant()))).ToList();
        
        // Clear body to avoid serialization issues
        foreach (var page in groupedPages) page.Body = "";
        
        var gridModel = new DataSourceResult {
            Data = groupedPages,
            Total = groupedPages.Count
        };

        return Json(gridModel);
    }

    #endregion

    #region Create / Edit / Delete

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new PageModel();
        //layouts
        await _pageViewModelService.PrepareLayoutsModel(model);
        //locales
        await AddLocales(_languageService, model.Locales);
        //default values
        model.DisplayOrder = 1;

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(PageModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            // Assign to current store
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
            
            var page = await _pageViewModelService.InsertPageModel(model);
            Success(_translationService.GetResource("Admin.Content.Pages.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = page.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        //layouts
        await _pageViewModelService.PrepareLayoutsModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var page = await _pageService.GetPageById(id);
        if (page == null)
            //No page found with the specified id
            return RedirectToAction("List");

        var storeId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        
        // Check if store has access to this page
        if (!page.AccessToEntityByStore(storeId))
            return RedirectToAction("List");

        // Warn if this is a global page or a shared page
        if (!page.LimitedToStores || (page.LimitedToStores && page.Stores.Contains(storeId) && page.Stores.Count > 1))
        {
            Warning(_translationService.GetResource("Admin.Content.Pages.Permissions"));
        }

        var model = page.ToModel(_dateTimeService);
        model.Url = Url.RouteUrl("Page", new { SeName = page.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id) }, "http");
        //layouts
        await _pageViewModelService.PrepareLayoutsModel(model);
        //locales
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
            //No page found with the specified id
            return RedirectToAction("List");

        var storeId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        
        if (!page.AccessToEntityByStore(storeId))
            return RedirectToAction("Edit", new { id = page.Id });

        if (ModelState.IsValid)
        {
            // If this is a global page (not limited to stores or doesn't belong to this store)
            // create a duplicate and assign it to the store
            if (!page.LimitedToStores || !page.Stores.Contains(storeId))
            {
                // Create a duplicate
                model.Id = ""; // Clear ID to create new
                model.Stores = [storeId];
                var newPage = await _pageViewModelService.InsertPageModel(model);
                Success(_translationService.GetResource("Admin.Content.Pages.Updated"));
                
                if (continueEditing)
                {
                    await SaveSelectedTabIndex();
                    return RedirectToAction("Edit", new { id = newPage.Id });
                }
                return RedirectToAction("List");
            }
            else
            {
                // Update existing store-specific page
                model.Stores = [storeId];
                page = await _pageViewModelService.UpdatePageModel(page, model);
                Success(_translationService.GetResource("Admin.Content.Pages.Updated"));
                
                if (continueEditing)
                {
                    await SaveSelectedTabIndex();
                    return RedirectToAction("Edit", new { id = page.Id });
                }
                return RedirectToAction("List");
            }
        }

        //If we got this far, something failed, redisplay form
        model.Url = Url.RouteUrl("Page", new { SeName = page.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id) }, "http");
        //layouts
        await _pageViewModelService.PrepareLayoutsModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var page = await _pageService.GetPageById(id);
        if (page == null)
            //No page found with the specified id
            return RedirectToAction("List");

        var storeId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        
        if (!page.AccessToEntityByStore(storeId))
            return RedirectToAction("Edit", new { id = page.Id });

        // Prevent deletion of global pages (not limited to stores)
        if (!page.LimitedToStores)
        {
            Error(_translationService.GetResource("Admin.Common.DeleteNotAllowed"));
            return RedirectToAction("Edit", new { id });
        }

        if (ModelState.IsValid)
        {
            await _pageViewModelService.DeletePage(page);
            Success(_translationService.GetResource("Admin.Content.Pages.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    #endregion
}
