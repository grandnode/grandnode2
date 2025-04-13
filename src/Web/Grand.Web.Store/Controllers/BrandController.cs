using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Brands)]
public class BrandController : BaseStoreController
{
    #region Constructors

    public BrandController(
        IBrandViewModelService brandViewModelService,
        IBrandService brandService,
        IContextAccessor contextAccessor,
        ILanguageService languageService,
        ITranslationService translationService,
        IPictureViewModelService pictureViewModelService)
    {
        _brandViewModelService = brandViewModelService;
        _brandService = brandService;
        _contextAccessor = contextAccessor;
        _languageService = languageService;
        _translationService = translationService;
        _pictureViewModelService = pictureViewModelService;
    }

    #endregion

    #region Fields

    private readonly IBrandViewModelService _brandViewModelService;
    private readonly IBrandService _brandService;
    private readonly IContextAccessor _contextAccessor;
    private readonly ILanguageService _languageService;
    private readonly ITranslationService _translationService;
    private readonly IPictureViewModelService _pictureViewModelService;

    #endregion

    #region List

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public IActionResult List()
    {
        var model = new BrandListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, BrandListModel model)
    {
        model.SearchStoreId = _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

        var brands = await _brandService.GetAllBrands(model.SearchBrandName, model.SearchStoreId, command.Page - 1, command.PageSize, true);
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
        model.PageSize = catalogSettings.DefaultPageSize;
        model.PageSizeOptions = catalogSettings.DefaultPageSizeOptions;
        model.Published = true;
        model.AllowCustomersToSelectPageSize = true;
        //sort options
        _brandViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(BrandModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
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

        if (!brand.LimitedToStores || (brand.LimitedToStores &&
                                       brand.Stores.Contains(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) &&
                                       brand.Stores.Count > 1))
        {
            Warning(_translationService.GetResource("Admin.Catalog.Brands.Permissions"));
        }
        else
        {
            if (!brand.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                return RedirectToAction("List");
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
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(BrandModel model, bool continueEditing)
    {
        var brand = await _brandService.GetBrandById(model.Id);
        if (brand == null)
            //No collection found with the specified id
            return RedirectToAction("List");

        if (!brand.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("Edit", new { id = brand.Id });

        if (ModelState.IsValid)
        {
            model.Stores = [_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId];
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

        if (!brand.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return RedirectToAction("Edit", new { id = brand.Id });

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

        if (!brand.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            return Content("This is not your brand");

        if (string.IsNullOrEmpty(brand.PictureId))
            return Content("Picture not exist");

        return View("Partials/PicturePopup",
            await _pictureViewModelService.PreparePictureModel(brand.PictureId, brand.Id));
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

            if (!brand.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                return Content("This is not your brand");

            if (string.IsNullOrEmpty(brand.PictureId))
                throw new ArgumentException("No picture found with the specified id");

            if (brand.PictureId != model.Id)
                throw new ArgumentException("Picture ident doesn't fit with brand");

            await _pictureViewModelService.UpdatePicture(model);

            return Content("");
        }

        Error(ModelState);

        return View("Partials/PicturePopup", model);
    }
    #endregion
}