using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
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

    #region Create / Edit / Delete

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await categoryViewModelService.PrepareCategoryModel(scope.DefaultStoreId);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(CategoryModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var category = await categoryViewModelService.InsertCategoryModel(model);
            Success(translationService.GetResource("Admin.Catalog.Categories.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = category.Id }) : RedirectToAction("List");
        }

        model = await categoryViewModelService.PrepareCategoryModel(model, null, scope.DefaultStoreId);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var category = await categoryService.GetCategoryById(id);
        if (category == null) return RedirectToAction("List");

        EditWarningCheck(category);
        // CanView, not HasAccess: viewing a shared/global category is allowed on Store (with a
        // warning from EditWarningCheck above); only mutating one is restricted to the exclusive
        // single-store owner. See IAdminDataScope<TEntity>.CanView's doc comment.
        if (!await scope.CanView(category)) return RedirectToAction("List");

        var model = category.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = category.GetTranslation(x => x.Name, languageId, false);
            locale.Description = category.GetTranslation(x => x.Description, languageId, false);
            locale.BottomDescription = category.GetTranslation(x => x.BottomDescription, languageId, false);
            locale.MetaKeywords = category.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = category.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = category.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = category.GetSeName(languageId, false);
            locale.Flag = category.GetTranslation(x => x.Flag, languageId, false);
        });
        model = await categoryViewModelService.PrepareCategoryModel(model, category, scope.DefaultStoreId);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(CategoryModel model, bool continueEditing)
    {
        var category = await categoryService.GetCategoryById(model.Id);
        if (category == null) return RedirectToAction("List");
        if (!await scope.HasAccess(category)) return RedirectToAction("Edit", new { id = category.Id });

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            category = await categoryViewModelService.UpdateCategoryModel(category, model);
            Success(translationService.GetResource("Admin.Catalog.Categories.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = category.Id });
            }
            return RedirectToAction("List");
        }

        model = await categoryViewModelService.PrepareCategoryModel(model, category, scope.DefaultStoreId);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var category = await categoryService.GetCategoryById(id);
        if (category == null) return RedirectToAction("List");
        if (!await scope.HasAccess(category)) return RedirectToAction("Edit", new { id = category.Id });

        if (ModelState.IsValid)
        {
            await categoryViewModelService.DeleteCategory(category);
            Success(translationService.GetResource("Admin.Catalog.Categories.Deleted"));
        }

        return RedirectToAction("List");
    }

    #endregion
}
