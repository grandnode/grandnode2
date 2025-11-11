using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Interfaces;
using Grand.Web.Store.Models.Pages;
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

    #endregion

    #region Constructors

    public PageController(
        IPageViewModelService pageViewModelService,
        IPageService pageService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _pageViewModelService = pageViewModelService;
        _pageService = pageService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
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
            .Select(x => new PageModel
            {
                Id = x.Id,
                SystemName = x.SystemName,
                Title = x.Title,
                Body = "", // Don't send body content to grid
                Published = x.Published,
                DisplayOrder = x.DisplayOrder,
                IsSystemPage = !string.IsNullOrEmpty(x.SystemName) && !x.Stores.Any()
            })
            .ToList();

        if (!string.IsNullOrEmpty(model.Name))
        {
            pageModels = pageModels.Where(x =>
                (x.SystemName != null && x.SystemName.Contains(model.Name, StringComparison.OrdinalIgnoreCase)) ||
                (x.Title != null && x.Title.Contains(model.Name, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        var gridModel = new DataSourceResult
        {
            Data = pageModels,
            Total = pageModels.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        var model = await _pageViewModelService.PreparePageModel(storeId);
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
                var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
                var page = await _pageViewModelService.InsertPageModel(model, storeId);
                Success(_translationService.GetResource("Store.Content.Pages.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = page.Id }) : RedirectToAction("List");
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

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

        var model = await _pageViewModelService.PreparePageModel(page, storeId);
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

        if (ModelState.IsValid)
        {
            try
            {
                var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
                page = await _pageViewModelService.UpdatePageModel(page, model, storeId);
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
            var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
            var newPage = await _pageViewModelService.CopyPageModel(id, storeId);
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
