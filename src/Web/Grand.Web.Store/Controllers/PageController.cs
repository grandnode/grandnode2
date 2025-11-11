using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
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
    #region Fields

    private readonly IPageViewModelService _pageViewModelService;
    private readonly IPageService _pageService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IDateTimeService _dateTimeService;

    #endregion

    #region Constructors

    public PageController(
        IPageViewModelService pageViewModelService,
        IPageService pageService,
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IDateTimeService dateTimeService)
    {
        _pageViewModelService = pageViewModelService;
        _pageService = pageService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
        _dateTimeService = dateTimeService;
    }

    #endregion

    #region Methods

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
        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        var pages = await _pageService.GetAllPages(storeId, true);

        var pageModels = pages
            .Select(x => {
                var pageModel = x.ToModel(_dateTimeService);
                pageModel.Body = ""; // Don't send body content to grid
                return pageModel;
            })
            .ToList();

        if (!string.IsNullOrEmpty(model.Name))
        {
            pageModels = pageModels.Where(x =>
                (x.SystemName != null && x.SystemName.Contains(model.Name, StringComparison.OrdinalIgnoreCase)) ||
                (x.Title != null && x.Title.Contains(model.Name, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        // Apply pagination
        var totalCount = pageModels.Count;
        var pagedData = pageModels
            .Skip((command.Page - 1) * command.PageSize)
            .Take(command.PageSize)
            .ToList();

        var gridModel = new DataSourceResult
        {
            Data = pagedData,
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new PageModel();
        await _pageViewModelService.PrepareLayoutsModel(model);
        model.DisplayOrder = 1;
        model.Published = true;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(PageModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Store-specific: Ensure SystemName is not set and page is limited to current store
                model.SystemName = null;
                
                var page = await _pageViewModelService.InsertPageModel(model);
                
                // Store-specific: Auto-tag to current store after creation
                var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
                page.LimitedToStores = true;
                page.Stores = new List<string> { storeId };
                await _pageService.UpdatePage(page);
                
                Success(_translationService.GetResource("Store.Content.Pages.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = page.Id }) : RedirectToAction("List");
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        await _pageViewModelService.PrepareLayoutsModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var page = await _pageService.GetPageById(id);
        if (page == null)
        {
            return RedirectToAction("List");
        }

        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        
        // Check if this is a system page or doesn't belong to this store
        var isSystemPage = !string.IsNullOrEmpty(page.SystemName) && !page.Stores.Any();
        var belongsToStore = page.Stores.Contains(storeId);

        if (isSystemPage || !belongsToStore)
        {
            // Redirect to copy action instead
            Warning(_translationService.GetResource("Store.Content.Pages.CannotEditSystemPage"));
            return RedirectToAction("List");
        }

        var model = page.ToModel(_dateTimeService);
        await _pageViewModelService.PrepareLayoutsModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(PageModel model, bool continueEditing)
    {
        var page = await _pageService.GetPageById(model.Id);
        if (page == null)
        {
            return RedirectToAction("List");
        }

        // Store-specific: Prevent editing system pages
        if (!string.IsNullOrEmpty(page.SystemName) && !page.Stores.Any())
        {
            Error(_translationService.GetResource("Store.Content.Pages.CannotEditSystemPage"));
            return RedirectToAction("List");
        }

        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        
        // Store-specific: Ensure page belongs to current store
        if (!page.Stores.Contains(storeId))
        {
            Error("Cannot edit a page that doesn't belong to this store.");
            return RedirectToAction("List");
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Store-specific: Ensure SystemName is not changed and page stays limited to current store
                model.SystemName = null;
                
                page = await _pageViewModelService.UpdatePageModel(page, model);
                
                // Store-specific: Maintain store restrictions
                page.LimitedToStores = true;
                page.Stores = new List<string> { storeId };
                await _pageService.UpdatePage(page);
                
                Success(_translationService.GetResource("Store.Content.Pages.Updated"));

                if (continueEditing)
                {
                    return RedirectToAction("Edit", new { id = page.Id });
                }

                return RedirectToAction("List");
            }
            catch (InvalidOperationException ex)
            {
                Error(ex.Message);
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    public async Task<IActionResult> Copy(string id)
    {
        var page = await _pageService.GetPageById(id);
        if (page == null)
        {
            return RedirectToAction("List");
        }

        try
        {
            var newPage = await _pageViewModelService.CopyPageModel(id);
            
            // Store-specific: Override copy to be store-specific and unpublished
            var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
            newPage.SystemName = null; // Don't copy system name
            newPage.Published = false; // Start unpublished
            newPage.LimitedToStores = true;
            newPage.Stores = new List<string> { storeId };
            newPage.IsPasswordProtected = false; // Don't copy password
            newPage.Password = null;
            await _pageService.UpdatePage(newPage);
            
            Success(_translationService.GetResource("Store.Content.Pages.Copied"));
            return RedirectToAction("Edit", new { id = newPage.Id });
        }
        catch (Exception ex)
        {
            Error(ex.Message);
            return RedirectToAction("List");
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var page = await _pageService.GetPageById(id);
        if (page == null)
        {
            return RedirectToAction("List");
        }

        try
        {
            await _pageViewModelService.DeletePage(page);
            Success(_translationService.GetResource("Store.Content.Pages.Deleted"));
        }
        catch (InvalidOperationException ex)
        {
            Error(ex.Message);
        }
        catch (Exception ex)
        {
            Error(ex.Message);
        }

        return RedirectToAction("List");
    }

    #endregion
}
