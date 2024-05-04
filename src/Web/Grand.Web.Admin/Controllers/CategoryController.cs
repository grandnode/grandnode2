using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Categories)]
public class CategoryController : BaseAdminController
{
    #region Constructors

    public CategoryController(
        ICategoryService categoryService,
        ICategoryViewModelService categoryViewModelService,
        ILanguageService languageService,
        ITranslationService translationService,
        IWorkContext workContext,
        IGroupService groupService,
        IPictureViewModelService pictureViewModelService)
    {
        _categoryService = categoryService;
        _categoryViewModelService = categoryViewModelService;
        _languageService = languageService;
        _translationService = translationService;
        _workContext = workContext;
        _groupService = groupService;
        _pictureViewModelService = pictureViewModelService;
    }

    #endregion

    #region Utilities

    protected async Task<(bool allow, string message)> CheckAccessToCategory(Category category)
    {
        if (category == null) return (false, "Category not exists");
        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            if (!(!category.LimitedToStores || (category.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) &&
                                                category.LimitedToStores)))
                return (false, "This is not your category");
        return (true, null);
    }

    #endregion

    #region Fields

    private readonly ICategoryService _categoryService;
    private readonly ICategoryViewModelService _categoryViewModelService;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;
    private readonly IGroupService _groupService;
    private readonly IPictureViewModelService _pictureViewModelService;

    #endregion

    #region List

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List()
    {
        var model = await _categoryViewModelService.PrepareCategoryListModel(_workContext.CurrentCustomer.StaffStoreId);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, CategoryListModel model)
    {
        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            model.SearchStoreId = _workContext.CurrentCustomer.StaffStoreId;

        var categories =
            await _categoryViewModelService.PrepareCategoryListModel(model, command.Page, command.PageSize);
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
        var model = await _categoryViewModelService.PrepareCategoryModel(_workContext.CurrentCustomer.StaffStoreId);
        //locales
        await AddLocales(_languageService, model.Locales);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(CategoryModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                model.Stores = [_workContext.CurrentCustomer.StaffStoreId];

            var category = await _categoryViewModelService.InsertCategoryModel(model);
            Success(_translationService.GetResource("Admin.Catalog.Categories.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = category.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        model = await _categoryViewModelService.PrepareCategoryModel(model, null,
            _workContext.CurrentCustomer.StaffStoreId);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var category = await _categoryService.GetCategoryById(id);
        if (category == null)
            //No category found with the specified id
            return RedirectToAction("List");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
        {
            if (!category.LimitedToStores || (category.LimitedToStores &&
                                              category.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) &&
                                              category.Stores.Count > 1))
            {
                Warning(_translationService.GetResource("Admin.Catalog.Categories.Permissions"));
            }
            else
            {
                if (!category.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("List");
            }
        }

        var model = category.ToModel();
        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
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
        model = await _categoryViewModelService.PrepareCategoryModel(model, category,
            _workContext.CurrentCustomer.StaffStoreId);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(CategoryModel model, bool continueEditing)
    {
        var category = await _categoryService.GetCategoryById(model.Id);
        if (category == null)
            //No category found with the specified id
            return RedirectToAction("List");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            if (!category.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                return RedirectToAction("Edit", new { id = category.Id });

        if (ModelState.IsValid)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                model.Stores = [_workContext.CurrentCustomer.StaffStoreId];

            category = await _categoryViewModelService.UpdateCategoryModel(category, model);

            Success(_translationService.GetResource("Admin.Catalog.Categories.Updated"));
            if (continueEditing)
            {
                //selected tab
                await SaveSelectedTabIndex();

                return RedirectToAction("Edit", new { id = category.Id });
            }

            return RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        model = await _categoryViewModelService.PrepareCategoryModel(model, category,
            _workContext.CurrentCustomer.StaffStoreId);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var category = await _categoryService.GetCategoryById(id);
        if (category == null)
            //No category found with the specified id
            return RedirectToAction("List");

        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            if (!category.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                return RedirectToAction("Edit", new { id = category.Id });

        if (ModelState.IsValid)
        {
            await _categoryViewModelService.DeleteCategory(category);
            Success(_translationService.GetResource("Admin.Catalog.Categories.Deleted"));
        }

        return RedirectToAction("List");
    }

    #endregion

    #region Picture

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> PicturePopup(string categoryId)
    {
        var category = await _categoryService.GetCategoryById(categoryId);
        if (category == null)
            return Content("Category not exist");

        if (string.IsNullOrEmpty(category.PictureId))
            return Content("Picture not exist");

        var permission = await CheckAccessToCategory(category);
        if (!permission.allow)
            return Content(permission.message);

        return View("Partials/PicturePopup",
            await _pictureViewModelService.PreparePictureModel(category.PictureId, category.Id));
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PicturePopup(PictureModel model)
    {
        if (ModelState.IsValid)
        {
            var category = await _categoryService.GetCategoryById(model.ObjectId);
            if (category == null)
                throw new ArgumentException("No category found with the specified id");

            if (string.IsNullOrEmpty(category.PictureId))
                throw new ArgumentException("No picture found with the specified id");

            if (category.PictureId != model.Id)
                throw new ArgumentException("Picture ident doesn't fit with category");

            await _pictureViewModelService.UpdatePicture(model);

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
            var bytes = await exportManager.Export(await _categoryService.GetAllCategories(showHidden: true,
                storeId: _workContext.CurrentCustomer.StaffStoreId));
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
                Error(_translationService.GetResource("Admin.Common.UploadFile"));
                return RedirectToAction("List");
            }

            Success(_translationService.GetResource("Admin.Catalog.Category.Imported"));
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
        var category = await _categoryService.GetCategoryById(categoryId);
        var permission = await CheckAccessToCategory(category);
        if (!permission.allow)
            return ErrorForKendoGridJson(permission.message);

        var productCategories =
            await _categoryViewModelService.PrepareCategoryProductModel(categoryId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = productCategories.categoryProductModels,
            Total = productCategories.totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductUpdate(CategoryModel.CategoryProductModel model)
    {
        if (ModelState.IsValid)
        {
            await _categoryViewModelService.UpdateProductCategoryModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductDelete(CategoryModel.CategoryProductModel model)
    {
        if (ModelState.IsValid)
        {
            await _categoryViewModelService.DeleteProductCategoryModel(model.Id, model.ProductId);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAddPopup(string categoryId)
    {
        var model = await _categoryViewModelService.PrepareAddCategoryProductModel(_workContext.CurrentCustomer
            .StaffStoreId);
        model.CategoryId = categoryId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command,
        CategoryModel.AddCategoryProductModel model)
    {
        var gridModel = new DataSourceResult();
        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            model.SearchStoreId = _workContext.CurrentCustomer.StaffStoreId;
        var products = await _categoryViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        gridModel.Data = products.products.ToList();
        gridModel.Total = products.totalCount;

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopup(CategoryModel.AddCategoryProductModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await _categoryViewModelService.InsertCategoryProductModel(model);

            return Content("");
        }

        Error(ModelState);
        return View(model);
    }

    #endregion
}