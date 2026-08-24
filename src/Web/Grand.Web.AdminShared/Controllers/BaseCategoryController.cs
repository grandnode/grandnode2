using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

// [AutoValidateAntiforgeryToken] is restated on each concrete host subclass (Admin/Store
// CategoryController) too - ASP.NET Core resolves filters from the concrete controller's full type
// hierarchy at runtime, so every real endpoint is already protected. It's added here as well,
// mirroring BaseProductController, so static analysis that doesn't follow the attribute across a
// base/derived project boundary has something to see in the same file as the actions.
[PermissionAuthorize(PermissionSystemName.Categories)]
[AutoValidateAntiforgeryToken]
public abstract class BaseCategoryController(
    ICategoryService categoryService,
    ICategoryViewModelService categoryViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IPictureViewModelService pictureViewModelService,
    IProductService productService,
    IAdminDataScope<Category> scope)
    : BaseController
{
    /// <summary>Hook for host-specific UI-copy warnings that aren't access-scope decisions.
    /// Overridden by the Store subclass (Task 3); no-op everywhere else. Mirrors
    /// BaseProductController.EditWarningCheck.</summary>
    protected virtual void EditWarningCheck(Category category) { }

    // Exposed for host subclasses: primary-constructor parameters are not visible to derived
    // classes by name in C#, so Store's EditWarningCheck override needs this.
    protected ITranslationService TranslationService => translationService;
    protected IAdminDataScope<Category> Scope => scope;

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        var model = await categoryViewModelService.PrepareCategoryListModel(scope.DefaultStoreId);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, CategoryListModel model)
    {
        model.SearchStoreId = scope.DefaultStoreId;
        var categories = await categoryViewModelService.PrepareCategoryListModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = categories.categoryListModel,
            Total = categories.totalCount
        };
        return Json(gridModel);
    }

    #endregion
}
