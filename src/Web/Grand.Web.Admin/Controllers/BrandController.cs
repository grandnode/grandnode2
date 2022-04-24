using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.System.ExportImport;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Brands)]
    public partial class BrandController : BaseAdminController
    {
        #region Fields
        private readonly IBrandViewModelService _brandViewModelService;
        private readonly IBrandService _brandService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IGroupService _groupService;
        private readonly IExportManager _exportManager;
        private readonly IImportManager _importManager;
        private readonly IPictureViewModelService _pictureViewModelService;
        #endregion

        #region Constructors

        public BrandController(
            IBrandViewModelService brandViewModelService,
            IBrandService brandService,
            IWorkContext workContext,
            IStoreService storeService,
            ILanguageService languageService,
            ITranslationService translationService,
            IGroupService groupService,
            IExportManager exportManager,
            IImportManager importManager,
            IPictureViewModelService pictureViewModelService)
        {
            _brandViewModelService = brandViewModelService;
            _brandService = brandService;
            _workContext = workContext;
            _storeService = storeService;
            _languageService = languageService;
            _translationService = translationService;
            _groupService = groupService;
            _exportManager = exportManager;
            _importManager = importManager;
            _pictureViewModelService = pictureViewModelService;
        }

        #endregion

        #region Utilities

        protected async Task<(bool allow, string message)> CheckAccessToBrand(Brand brand)
        {
            if (brand == null)
            {
                return (false, "Brand not exists");
            }
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!(!brand.LimitedToStores || (brand.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && brand.LimitedToStores)))
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
            var model = new BrandListModel();
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in (await _storeService.GetAllStores()).Where(x => x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, BrandListModel model)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.SearchStoreId = _workContext.CurrentCustomer.StaffStoreId;
            }
            var brands = await _brandService.GetAllBrands(model.SearchBrandName,
                model.SearchStoreId, command.Page - 1, command.PageSize, true);
            var gridModel = new DataSourceResult {
                Data = brands.Select(x => x.ToModel()),
                Total = brands.TotalCount
            };

            return Json(gridModel);
        }

        #endregion

        #region Create / Edit / Delete

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create([FromServices] CatalogSettings catalogSettings)
        {
            var model = new BrandModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            //layouts
            await _brandViewModelService.PrepareLayoutsModel(model);
            //discounts
            await _brandViewModelService.PrepareDiscountModel(model, null, true);
            //default values
            model.PageSize = catalogSettings.DefaultCollectionPageSize;
            model.PageSizeOptions = catalogSettings.DefaultCollectionPageSizeOptions;
            model.Published = true;
            model.AllowCustomersToSelectPageSize = true;
            //sort options
            _brandViewModelService.PrepareSortOptionsModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(BrandModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }

                var collection = await _brandViewModelService.InsertBrandModel(model);
                Success(_translationService.GetResource("Admin.Catalog.Brands.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = collection.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //layouts
            await _brandViewModelService.PrepareLayoutsModel(model);
            //discounts
            await _brandViewModelService.PrepareDiscountModel(model, null, true);
            //sort options
            _brandViewModelService.PrepareSortOptionsModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var brand = await _brandService.GetBrandById(id);
            if (brand == null)
                //No collection found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!brand.LimitedToStores || (brand.LimitedToStores && brand.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && brand.Stores.Count > 1))
                    Warning(_translationService.GetResource("Admin.Catalog.Brands.Permisions"));
                else
                {
                    if (!brand.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                        return RedirectToAction("List");
                }
            }

            var model = brand.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = brand.GetTranslation(x => x.Name, languageId, false);
                locale.Description = brand.GetTranslation(x => x.Description, languageId, false);
                locale.BottomDescription = brand.GetTranslation(x => x.BottomDescription, languageId, false);
                locale.MetaKeywords = brand.GetTranslation(x => x.MetaKeywords, languageId, false);
                locale.MetaDescription = brand.GetTranslation(x => x.MetaDescription, languageId, false);
                locale.MetaTitle = brand.GetTranslation(x => x.MetaTitle, languageId, false);
                locale.SeName = brand.GetSeName(languageId, false);
            });
            //layouts
            await _brandViewModelService.PrepareLayoutsModel(model);
            //discounts
            await _brandViewModelService.PrepareDiscountModel(model, brand, false);
            //sort options
            _brandViewModelService.PrepareSortOptionsModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(BrandModel model, bool continueEditing)
        {
            var brand = await _brandService.GetBrandById(model.Id);
            if (brand == null)
                //No collection found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!brand.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = brand.Id });
            }
            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }
                brand = await _brandViewModelService.UpdateBrandModel(brand, model);
                Success(_translationService.GetResource("Admin.Catalog.Brands.Updated"));

                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = brand.Id });
                }
                return RedirectToAction("List");
            }


            //If we got this far, something failed, redisplay form
            //layouts
            await _brandViewModelService.PrepareLayoutsModel(model);
            //discounts
            await _brandViewModelService.PrepareDiscountModel(model, brand, true);
            //sort options
            _brandViewModelService.PrepareSortOptionsModel(model);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var brand = await _brandService.GetBrandById(id);
            if (brand == null)
                //No collection found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!brand.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = brand.Id });
            }

            if (ModelState.IsValid)
            {
                await _brandViewModelService.DeleteBrand(brand);

                Success(_translationService.GetResource("Admin.Catalog.Brands.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = brand.Id });
        }

        #endregion

        #region Picture

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> PicturePopup(string brandId)
        {
            var brand = await _brandService.GetBrandById(brandId);
            if (brand == null)
                return Content("Brand not exist");

            if (string.IsNullOrEmpty(brand.PictureId))
                return Content("Picture not exist");

            var permission = await CheckAccessToBrand(brand);
            if (!permission.allow)
                return Content(permission.message);

            return View("PicturePopup", await _pictureViewModelService.PreparePictureModel(brand.PictureId, brand.Id));
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> PicturePopup(PictureModel model)
        {
            if (ModelState.IsValid)
            {
                var brand = await _brandService.GetBrandById(model.ObjectId);
                if (brand == null)
                    throw new ArgumentException("No brand found with the specified id");

                var permission = await CheckAccessToBrand(brand);
                if (!permission.allow)
                    return Content(permission.message);

                if (string.IsNullOrEmpty(brand.PictureId))
                    throw new ArgumentException("No picture found with the specified id");

                if (brand.PictureId != model.Id)
                    throw new ArgumentException("Picture ident doesn't fit with brand");

                await _pictureViewModelService.UpdatePicture(model);

                return Content("");
            }

            Error(ModelState);

            return View("PicturePopup", model);
        }

        #endregion

        #region Export / Import

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        public async Task<IActionResult> ExportXlsx()
        {
            try
            {
                var bytes = _exportManager.ExportBrandsToXlsx(await _brandService.GetAllBrands(showHidden: true, storeId: _workContext.CurrentCustomer.StaffStoreId));
                return File(bytes, "text/xls", "brands.xlsx");
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
                    await _importManager.ImportBrandFromXlsx(importexcelfile.OpenReadStream());
                }
                else
                {
                    Error(_translationService.GetResource("Admin.Common.UploadFile"));
                    return RedirectToAction("List");
                }
                Success(_translationService.GetResource("Admin.Catalog.Brands.Imported"));
                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("List");
            }
        }
        #endregion

        #region Activity log

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> ListActivityLog(DataSourceRequest command, string brandId)
        {
            var brand = await _brandService.GetBrandById(brandId);
            var permission = await CheckAccessToBrand(brand);
            if (!permission.allow)
                return ErrorForKendoGridJson(permission.message);

            var (activityLogModels, totalCount) = await _brandViewModelService.PrepareActivityLogModel(brandId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult {
                Data = activityLogModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }
        #endregion
    }

}
