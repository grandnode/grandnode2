using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Menu;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Maintenance)]
public class MenuController : BaseAdminController
{
    #region Ctor

    public MenuController(
        IMenuViewModelService menuViewModelService,
        ITranslationService translationService)
    {
        _menuViewModelService = menuViewModelService;
        _translationService = translationService;
    }

    #endregion

    #region Fields

    private readonly IMenuViewModelService _menuViewModelService;
    private readonly ITranslationService _translationService;

    #endregion

    #region Methods

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    [HttpGet]
    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ListItem()
    {
        var model = await _menuViewModelService.MenuItems();
        var gridModel = new DataSourceResult {
            Data = model,
            Total = model.Count
        };
        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> ChildItem(string parentId)
    {
        var model = await _menuViewModelService.GetMenuById(parentId);
        var gridModel = new DataSourceResult {
            Data = model.ChildNodes,
            Total = model.ChildNodes.Count
        };
        return Json(gridModel);
    }

    #region Create / Edit / Delete

    public IActionResult Create()
    {
        var model = new MenuModel();
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(MenuModel model, bool continueEditing, string parentId)
    {
        if (ModelState.IsValid)
        {
            var menu = await _menuViewModelService.InsertMenuModel(model, parentId);
            Success(_translationService.GetResource("Admin.Configuration.Menu.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = menu.Id }) : RedirectToAction("Index");
        }

        //If we got this far, something failed, redisplay form
        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var menu = await _menuViewModelService.GetMenuById(id);
        if (menu == null)
            //No menu found with the specified id
            return RedirectToAction("Index");

        var model = menu.ToModel();

        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(MenuModel model, bool continueEditing)
    {
        var menu = await _menuViewModelService.GetMenuById(model.Id);
        if (menu == null)
            //No menu found with the specified id
            return RedirectToAction("Index");

        if (!ModelState.IsValid) return View(model);

        await _menuViewModelService.UpdateMenuModel(model);
        Success(_translationService.GetResource("Admin.Configuration.Menu.Updated"));

        if (!continueEditing) return RedirectToAction("List");

        //selected tab
        await SaveSelectedTabIndex();
        return RedirectToAction("Edit", new { id = menu.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var menu = await _menuViewModelService.GetMenuById(id);
        if (menu == null)
            //No menu found with the specified id
            return RedirectToAction("Index");

        if (ModelState.IsValid)
        {
            await _menuViewModelService.DeleteMenu(id);

            Success(_translationService.GetResource("Admin.Configuration.Menu.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    #endregion

    #endregion
}