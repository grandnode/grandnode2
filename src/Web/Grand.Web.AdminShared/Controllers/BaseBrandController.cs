using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
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
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

// [AutoValidateAntiforgeryToken] is restated on each concrete host subclass (Admin/Store
// BrandController) too - ASP.NET Core resolves filters from the concrete controller's full type
// hierarchy at runtime, so every real endpoint is already protected. It's added here as well,
// mirroring BaseProductController/BaseCategoryController/BaseCollectionController, so static
// analysis that doesn't follow the attribute across a base/derived project boundary has something
// to see in the same file as the actions.
[PermissionAuthorize(PermissionSystemName.Brands)]
[AutoValidateAntiforgeryToken]
public abstract class BaseBrandController(
    IBrandViewModelService brandViewModelService,
    IBrandService brandService,
    IStoreService storeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IPictureViewModelService pictureViewModelService,
    IAdminDataScope<Brand> scope)
    : BaseController
{
    /// <summary>Hook for host-specific UI-copy warnings that aren't access-scope decisions.
    /// Overridden by the Store subclass (Task 6); no-op everywhere else. Mirrors
    /// BaseCategoryController.EditWarningCheck/BaseCollectionController's equivalent.</summary>
    protected virtual void EditWarningCheck(Brand brand) { }

    // Exposed for host subclasses: primary-constructor parameters are not visible to derived
    // classes by name in C#, so Store's EditWarningCheck override needs these.
    protected ITranslationService TranslationService => translationService;
    protected IAdminDataScope<Brand> Scope => scope;

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        var model = new BrandListModel();
        // Admin only: Store never had this dropdown (it's implicitly single-store).
        // ShowStoreSelector can't gate this - it's true on both Global and Store scopes.
        if (scope.DefaultStoreId is null)
        {
            model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in await storeService.GetAllStores())
            {
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
            }
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, BrandListModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;
        var brands = await brandService.GetAllBrands(model.SearchBrandName,
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
        await AddLocales(languageService, model.Locales);
        await brandViewModelService.PrepareLayoutsModel(model);
        await brandViewModelService.PrepareDiscountModel(model, null, true);
        model.PageSize = catalogSettings.DefaultPageSize;
        model.PageSizeOptions = catalogSettings.DefaultPageSizeOptions;
        model.Published = true;
        model.AllowCustomersToSelectPageSize = true;
        brandViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(BrandModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var brand = await brandViewModelService.InsertBrandModel(model);
            Success(translationService.GetResource("Admin.Catalog.Brands.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = brand.Id }) : RedirectToAction("List");
        }

        await brandViewModelService.PrepareLayoutsModel(model);
        await brandViewModelService.PrepareDiscountModel(model, null, true);
        brandViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var brand = await brandService.GetBrandById(id);
        if (brand == null) return RedirectToAction("List");

        EditWarningCheck(brand);
        // CanView, not HasAccess: viewing a shared/global brand is allowed on Store (with a
        // warning from EditWarningCheck above); only mutating one is restricted to the exclusive
        // single-store owner. See IAdminDataScope<TEntity>.CanView's doc comment.
        if (!await scope.CanView(brand)) return RedirectToAction("List");

        var model = brand.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = brand.GetTranslation(x => x.Name, languageId, false);
            locale.Description = brand.GetTranslation(x => x.Description, languageId, false);
            locale.BottomDescription = brand.GetTranslation(x => x.BottomDescription, languageId, false);
            locale.MetaKeywords = brand.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = brand.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = brand.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = brand.GetSeName(languageId, false);
        });
        await brandViewModelService.PrepareLayoutsModel(model);
        await brandViewModelService.PrepareDiscountModel(model, brand, false);
        brandViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(BrandModel model, bool continueEditing)
    {
        var brand = await brandService.GetBrandById(model.Id);
        if (brand == null) return RedirectToAction("List");
        if (!await scope.HasAccess(brand)) return RedirectToAction("Edit", new { id = brand.Id });

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            brand = await brandViewModelService.UpdateBrandModel(brand, model);
            Success(translationService.GetResource("Admin.Catalog.Brands.Updated"));

            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = brand.Id });
            }
            return RedirectToAction("List");
        }

        await brandViewModelService.PrepareLayoutsModel(model);
        await brandViewModelService.PrepareDiscountModel(model, brand, true);
        brandViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var brand = await brandService.GetBrandById(id);
        if (brand == null) return RedirectToAction("List");
        if (!await scope.HasAccess(brand)) return RedirectToAction("Edit", new { id = brand.Id });

        if (ModelState.IsValid)
        {
            await brandViewModelService.DeleteBrand(brand);
            Success(translationService.GetResource("Admin.Catalog.Brands.Deleted"));
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
        var brand = await brandService.GetBrandById(brandId);
        if (brand == null) return Content("Brand not exist");
        if (!await scope.HasAccess(brand)) return Content("This is not your brand");
        if (string.IsNullOrEmpty(brand.PictureId)) return Content("Picture not exist");

        return View("Partials/PicturePopup",
            await pictureViewModelService.PreparePictureModel(brand.PictureId, brand.Id));
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PicturePopup(PictureModel model)
    {
        if (ModelState.IsValid)
        {
            var brand = await brandService.GetBrandById(model.ObjectId);
            if (brand == null)
                throw new ArgumentException("No brand found with the specified id");
            if (!await scope.HasAccess(brand)) return Content("This is not your brand");
            if (string.IsNullOrEmpty(brand.PictureId))
                throw new ArgumentException("No picture found with the specified id");
            if (brand.PictureId != model.Id)
                throw new ArgumentException("Picture ident doesn't fit with brand");

            await pictureViewModelService.UpdatePicture(model);
            return Content("");
        }

        Error(ModelState);
        return View("Partials/PicturePopup", model);
    }

    #endregion

    #region Export / Import

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    public async Task<IActionResult> ExportXlsx([FromServices] IExportManager<Brand> exportManager)
    {
        try
        {
            var bytes = await exportManager.Export(await brandService.GetAllBrands(brandName: "", storeId: scope.DefaultStoreId ?? "", showHidden: true));
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
    public async Task<IActionResult> ImportFromXlsx(IFormFile importexcelfile,
        [FromServices] IImportManager<BrandDto> importManager)
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

            Success(translationService.GetResource("Admin.Catalog.Brands.Imported"));
            return RedirectToAction("List");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    #endregion
}
