using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
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

    #region Picture

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> PicturePopup(string categoryId)
    {
        var category = await categoryService.GetCategoryById(categoryId);
        if (category == null) return Content("Category not exist");
        if (!await scope.HasAccess(category)) return Content("This is not your category");
        if (string.IsNullOrEmpty(category.PictureId)) return Content("Picture not exist");

        return View("Partials/PicturePopup",
            await pictureViewModelService.PreparePictureModel(category.PictureId, category.Id));
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PicturePopup(PictureModel model)
    {
        if (ModelState.IsValid)
        {
            var category = await categoryService.GetCategoryById(model.ObjectId);
            if (category == null)
                throw new ArgumentException("No category found with the specified id");
            if (!await scope.HasAccess(category)) return Content("This is not your category");
            if (string.IsNullOrEmpty(category.PictureId))
                throw new ArgumentException("No picture found with the specified id");
            if (category.PictureId != model.Id)
                throw new ArgumentException("Picture ident doesn't fit with category");

            await pictureViewModelService.UpdatePicture(model);
            return Content("");
        }

        Error(ModelState);
        return View("Partials/PicturePopup", model);
    }

    #endregion

    #region Export / Import

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    public async Task<IActionResult> ExportXlsx([FromServices] IExportManager<Category> exportManager)
    {
        try
        {
            var bytes = await exportManager.Export(await categoryService.GetAllCategories(parentId: null, categoryName: "", storeId: scope.DefaultStoreId ?? "", showHidden: true));
            return File(bytes, "text/xls", "categories.xlsx");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Import)]
    [HttpPost]
    public async Task<IActionResult> ImportFromXlsx(IFormFile importexcelfile,
        [FromServices] IImportManager<CategoryDto> importManager)
    {
        try
        {
            if (importexcelfile is { Length: > 0 })
            {
                await importManager.Import(importexcelfile.OpenReadStream());
            }
            else
            {
                Error(translationService.GetResource("Admin.Common.UploadFile"));
                return RedirectToAction("List");
            }

            Success(translationService.GetResource("Admin.Catalog.Category.Imported"));
            return RedirectToAction("List");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    #endregion

    #region Products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductList(DataSourceRequest command, string categoryId)
    {
        var category = await categoryService.GetCategoryById(categoryId);
        if (!await scope.HasAccess(category)) return ErrorForKendoGridJson("This is not your category");

        var productCategories = await categoryViewModelService.PrepareCategoryProductModel(categoryId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = productCategories.categoryProductModels,
            Total = productCategories.totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductUpdate(CategoryModel.CategoryProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null || !product.AccessToEntityByStore(scope.DefaultStoreId))
            return ErrorForKendoGridJson("This is not your product");

        if (ModelState.IsValid)
        {
            await categoryViewModelService.UpdateProductCategoryModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductDelete(CategoryModel.CategoryProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null || !product.AccessToEntityByStore(scope.DefaultStoreId))
            return ErrorForKendoGridJson("This is not your product");

        if (ModelState.IsValid)
        {
            await categoryViewModelService.DeleteProductCategoryModel(model.Id, model.ProductId);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAddPopup(string categoryId)
    {
        var model = await categoryViewModelService.PrepareAddCategoryProductModel(scope.DefaultStoreId);
        model.CategoryId = categoryId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CategoryModel.AddCategoryProductModel model)
    {
        var gridModel = new DataSourceResult();
        model.SearchStoreId = scope.DefaultStoreId;
        var products = await categoryViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        gridModel.Data = products.products.ToList();
        gridModel.Total = products.totalCount;
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopup(CategoryModel.AddCategoryProductModel model)
    {
        var category = await categoryService.GetCategoryById(model.CategoryId);
        if (category == null || !await scope.HasAccess(category))
            return Content("This is not your category");

        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null)
            {
                if (scope.DefaultStoreId is null)
                {
                    // Global scope (Admin): no per-product ownership concept, insert as submitted -
                    // matches Admin's original, unfiltered behavior exactly.
                    await categoryViewModelService.InsertCategoryProductModel(model);
                }
                else
                {
                    // Store scope: InsertCategoryProductModel mutates each selected product's
                    // ProductCategories collection, so every selected id must also belong to the
                    // current store - matches Store's original filtering loop exactly.
                    var validIds = new List<string>();
                    foreach (var id in model.SelectedProductIds)
                    {
                        var selected = await productService.GetProductById(id);
                        if (selected != null && selected.AccessToEntityByStore(scope.DefaultStoreId))
                            validIds.Add(id);
                    }
                    model.SelectedProductIds = validIds.ToArray();
                    if (validIds.Count > 0) await categoryViewModelService.InsertCategoryProductModel(model);
                }
            }

            return Content("");
        }

        Error(ModelState);
        return View(model);
    }

    #endregion
}
