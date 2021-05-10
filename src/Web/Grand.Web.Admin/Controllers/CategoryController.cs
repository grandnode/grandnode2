using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.System.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Categories)]
    public partial class CategoryController : BaseAdminController
    {
        #region Fields

        private readonly ICategoryService _categoryService;
        private readonly ICategoryViewModelService _categoryViewModelService;
        private readonly ICustomerService _customerService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IStoreService _storeService;
        private readonly IExportManager _exportManager;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly IImportManager _importManager;

        #endregion

        #region Constructors

        public CategoryController(
            ICategoryService categoryService,
            ICategoryViewModelService categoryViewModelService,
            ICustomerService customerService,
            ILanguageService languageService,
            ITranslationService translationService,
            IStoreService storeService,
            IExportManager exportManager,
            IWorkContext workContext,
            IGroupService groupService,
            IImportManager importManager)
        {
            _categoryService = categoryService;
            _categoryViewModelService = categoryViewModelService;
            _customerService = customerService;
            _languageService = languageService;
            _translationService = translationService;
            _storeService = storeService;
            _exportManager = exportManager;
            _workContext = workContext;
            _groupService = groupService;
            _importManager = importManager;
        }

        #endregion

        #region Utilities

        protected async Task<(bool allow, string message)> CheckAccessToCategory(Category category)
        {
            if (category == null)
            {
                return (false, "Category not exists");
            }
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!(!category.LimitedToStores || (category.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && category.LimitedToStores)))
                    return (false, "This is not your category");
            }
            return (true, null);
        }

        #endregion

        #region List 

        public IActionResult Index() => RedirectToAction("List");

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
            {
                model.SearchStoreId = _workContext.CurrentCustomer.StaffStoreId;
            }

            var categories = await _categoryViewModelService.PrepareCategoryListModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
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
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(CategoryModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }

                var category = await _categoryViewModelService.InsertCategoryModel(model);
                Success(_translationService.GetResource("Admin.Catalog.Categories.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = category.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            model = await _categoryViewModelService.PrepareCategoryModel(model, null, _workContext.CurrentCustomer.StaffStoreId);

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
                if (!category.LimitedToStores || (category.LimitedToStores && category.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && category.Stores.Count > 1))
                    Warning(_translationService.GetResource("Admin.Catalog.Categories.Permisions"));
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
            model = await _categoryViewModelService.PrepareCategoryModel(model, category, _workContext.CurrentCustomer.StaffStoreId);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(CategoryModel model, bool continueEditing)
        {
            var category = await _categoryService.GetCategoryById(model.Id);
            if (category == null)
                //No category found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!category.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = category.Id });
            }

            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }

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
            model = await _categoryViewModelService.PrepareCategoryModel(model, category, _workContext.CurrentCustomer.StaffStoreId);

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
            {
                if (!category.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = category.Id });
            }

            if (ModelState.IsValid)
            {
                await _categoryViewModelService.DeleteCategory(category);
                Success(_translationService.GetResource("Admin.Catalog.Categories.Deleted"));
            }
            return RedirectToAction("List");
        }


        #endregion

        #region Export / Import


        [PermissionAuthorizeAction(PermissionActionName.Export)]
        public async Task<IActionResult> ExportXlsx()
        {
            try
            {
                var bytes = _exportManager.ExportCategoriesToXlsx(await _categoryService.GetAllCategories(showHidden: true, storeId: _workContext.CurrentCustomer.StaffStoreId));
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
        public async Task<IActionResult> ImportFromXlsx(IFormFile importexcelfile)
        {
            //a vendor and staff cannot import categories
            if (_workContext.CurrentVendor != null || await _groupService.IsStaff(_workContext.CurrentCustomer))
                return AccessDeniedView();

            try
            {
                if (importexcelfile != null && importexcelfile.Length > 0)
                {
                    await _importManager.ImportCategoryFromXlsx(importexcelfile.OpenReadStream());
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

            var productCategories = await _categoryViewModelService.PrepareCategoryProductModel(categoryId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
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
            var model = await _categoryViewModelService.PrepareAddCategoryProductModel(_workContext.CurrentCustomer.StaffStoreId);
            model.CategoryId = categoryId;
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CategoryModel.AddCategoryProductModel model)
        {
            var gridModel = new DataSourceResult();
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.SearchStoreId = _workContext.CurrentCustomer.StaffStoreId;
            }
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
                if (model.SelectedProductIds != null)
                {
                    await _categoryViewModelService.InsertCategoryProductModel(model);
                }
                ViewBag.RefreshPage = true;
            }
            else
                Error(ModelState);

            return View(model);
        }

        #endregion

        #region Activity log

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> ListActivityLog(DataSourceRequest command, string categoryId)
        {
            var category = await _categoryService.GetCategoryById(categoryId);

            var permission = await CheckAccessToCategory(category);
            if (!permission.allow)
                return ErrorForKendoGridJson(permission.message);

            var activityLog = await _categoryViewModelService.PrepareActivityLogModel(categoryId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = activityLog.activityLogModel,
                Total = activityLog.totalCount
            };

            return Json(gridModel);
        }

        #endregion
    }
}
