using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
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
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Collections)]
    public partial class CollectionController : BaseAdminController
    {
        #region Fields
        private readonly ICollectionViewModelService _collectionViewModelService;
        private readonly ICollectionService _collectionService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IGroupService _groupService;
        private readonly IExportManager _exportManager;
        private readonly IImportManager _importManager;
        #endregion

        #region Constructors

        public CollectionController(
            ICollectionViewModelService collectionViewModelService,
            ICollectionService collectionService,
            IWorkContext workContext,
            IStoreService storeService,
            ILanguageService languageService,
            ITranslationService translationService,
            IGroupService groupService,
            IExportManager exportManager,
            IImportManager importManager)
        {
            _collectionViewModelService = collectionViewModelService;
            _collectionService = collectionService;
            _workContext = workContext;
            _storeService = storeService;
            _languageService = languageService;
            _translationService = translationService;
            _groupService = groupService;
            _exportManager = exportManager;
            _importManager = importManager;
        }

        #endregion

        #region Utilities

        protected async Task<(bool allow, string message)> CheckAccessToCollection(Collection collection)
        {
            if (collection == null)
            {
                return (false, "Collection not exists");
            }
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!(!collection.LimitedToStores || (collection.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && collection.LimitedToStores)))
                    return (false, "This is not your collection");
            }
            return (true, null);
        }

        #endregion

        #region List

        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List()
        {
            var storeId = _workContext.CurrentCustomer.StaffStoreId;
            var model = new CollectionListModel();
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in (await _storeService.GetAllStores()).Where(x => x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, CollectionListModel model)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.SearchStoreId = _workContext.CurrentCustomer.StaffStoreId;
            }
            var collections = await _collectionService.GetAllCollections(model.SearchCollectionName,
                model.SearchStoreId, command.Page - 1, command.PageSize, true);
            var gridModel = new DataSourceResult
            {
                Data = collections.Select(x => x.ToModel()),
                Total = collections.TotalCount
            };

            return Json(gridModel);
        }

        #endregion

        #region Create / Edit / Delete

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create([FromServices] CatalogSettings catalogSettings)
        {
            var model = new CollectionModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            //layouts
            await _collectionViewModelService.PrepareLayoutsModel(model);
            //discounts
            await _collectionViewModelService.PrepareDiscountModel(model, null, true);
            //default values
            model.PageSize = catalogSettings.DefaultCollectionPageSize;
            model.PageSizeOptions = catalogSettings.DefaultCollectionPageSizeOptions;
            model.Published = true;
            model.AllowCustomersToSelectPageSize = true;
            //sort options
            _collectionViewModelService.PrepareSortOptionsModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(CollectionModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }

                var collection = await _collectionViewModelService.InsertCollectionModel(model);
                Success(_translationService.GetResource("Admin.Catalog.Collections.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = collection.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //layouts
            await _collectionViewModelService.PrepareLayoutsModel(model);
            //discounts
            await _collectionViewModelService.PrepareDiscountModel(model, null, true);
            //sort options
            _collectionViewModelService.PrepareSortOptionsModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var collection = await _collectionService.GetCollectionById(id);
            if (collection == null)
                //No collection found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!collection.LimitedToStores || (collection.LimitedToStores && collection.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && collection.Stores.Count > 1))
                    Warning(_translationService.GetResource("Admin.Catalog.Collections.Permisions"));
                else
                {
                    if (!collection.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                        return RedirectToAction("List");
                }
            }

            var model = collection.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = collection.GetTranslation(x => x.Name, languageId, false);
                locale.Description = collection.GetTranslation(x => x.Description, languageId, false);
                locale.BottomDescription = collection.GetTranslation(x => x.BottomDescription, languageId, false);
                locale.MetaKeywords = collection.GetTranslation(x => x.MetaKeywords, languageId, false);
                locale.MetaDescription = collection.GetTranslation(x => x.MetaDescription, languageId, false);
                locale.MetaTitle = collection.GetTranslation(x => x.MetaTitle, languageId, false);
                locale.SeName = collection.GetSeName(languageId, false);
            });
            //layouts
            await _collectionViewModelService.PrepareLayoutsModel(model);
            //discounts
            await _collectionViewModelService.PrepareDiscountModel(model, collection, false);
            //sort options
            _collectionViewModelService.PrepareSortOptionsModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(CollectionModel model, bool continueEditing)
        {
            var collection = await _collectionService.GetCollectionById(model.Id);
            if (collection == null)
                //No collection found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!collection.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = collection.Id });
            }
            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }
                collection = await _collectionViewModelService.UpdateCollectionModel(collection, model);
                Success(_translationService.GetResource("Admin.Catalog.Collections.Updated"));

                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = collection.Id });
                }
                return RedirectToAction("List");
            }


            //If we got this far, something failed, redisplay form
            //layouts
            await _collectionViewModelService.PrepareLayoutsModel(model);
            //discounts
            await _collectionViewModelService.PrepareDiscountModel(model, collection, true);
            //sort options
            _collectionViewModelService.PrepareSortOptionsModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var collection = await _collectionService.GetCollectionById(id);
            if (collection == null)
                //No collection found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!collection.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = collection.Id });
            }

            if (ModelState.IsValid)
            {
                await _collectionViewModelService.DeleteCollection(collection);

                Success(_translationService.GetResource("Admin.Catalog.Collections.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = collection.Id });
        }

        #endregion

        #region Export / Import

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        public async Task<IActionResult> ExportXlsx()
        {
            try
            {
                var bytes = _exportManager.ExportCollectionsToXlsx(await _collectionService.GetAllCollections(showHidden: true, storeId: _workContext.CurrentCustomer.StaffStoreId));
                return File(bytes, "text/xls", "collections.xlsx");
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("List");
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Import)]
        [HttpPost]
        public async Task<IActionResult> ImportFromXlsx(IFormFile importexcelfile, [FromServices] IWorkContext workContext)
        {
            //a vendor and staff cannot import collections
            if (workContext.CurrentVendor != null || await _groupService.IsStaff(_workContext.CurrentCustomer))
                return AccessDeniedView();

            try
            {
                if (importexcelfile != null && importexcelfile.Length > 0)
                {
                    await _importManager.ImportCollectionFromXlsx(importexcelfile.OpenReadStream());
                }
                else
                {
                    Error(_translationService.GetResource("Admin.Common.UploadFile"));
                    return RedirectToAction("List");
                }
                Success(_translationService.GetResource("Admin.Catalog.Collection.Imported"));
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
        public async Task<IActionResult> ProductList(DataSourceRequest command, string collectionId)
        {
            var collection = await _collectionService.GetCollectionById(collectionId);
            var permission = await CheckAccessToCollection(collection);
            if (!permission.allow)
                return ErrorForKendoGridJson(permission.message);

            var (collectionProductModels, totalCount) = await _collectionViewModelService.PrepareCollectionProductModel(collectionId, _workContext.CurrentStore.Id, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = collectionProductModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductUpdate(CollectionModel.CollectionProductModel model)
        {
            if (ModelState.IsValid)
            {
                await _collectionViewModelService.ProductUpdate(model);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductDelete(CollectionModel.CollectionProductModel model)
        {
            if (ModelState.IsValid)
            {
                await _collectionViewModelService.ProductDelete(model.Id, model.ProductId);
                return new JsonResult("");
            }

            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductAddPopup(string collectionId)
        {
            var model = await _collectionViewModelService.PrepareAddCollectionProductModel(_workContext.CurrentCustomer.StaffStoreId);
            model.CollectionId = collectionId;
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CollectionModel.AddCollectionProductModel model)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.SearchStoreId = _workContext.CurrentCustomer.StaffStoreId;
            }
            var products = await _collectionViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = products.products.ToList(),
                Total = products.totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopup(CollectionModel.AddCollectionProductModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.SelectedProductIds != null)
                {
                    await _collectionViewModelService.InsertCollectionProductModel(model);
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
        public async Task<IActionResult> ListActivityLog(DataSourceRequest command, string collectionId)
        {
            var collection = await _collectionService.GetCollectionById(collectionId);
            var permission = await CheckAccessToCollection(collection);
            if (!permission.allow)
                return ErrorForKendoGridJson(permission.message);

            var (activityLogModels, totalCount) = await _collectionViewModelService.PrepareActivityLogModel(collectionId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = activityLogModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }
        #endregion
    }

}
