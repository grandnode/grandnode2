using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

// [AutoValidateAntiforgeryToken] is restated on each concrete host subclass (Admin/Store
// CollectionController) too - ASP.NET Core resolves filters from the concrete controller's full
// type hierarchy at runtime, so every real endpoint is already protected. It's added here as well,
// mirroring BaseProductController/BaseCategoryController, so static analysis that doesn't follow
// the attribute across a base/derived project boundary has something to see in the same file as
// the actions.
[PermissionAuthorize(PermissionSystemName.Collections)]
[AutoValidateAntiforgeryToken]
public abstract class BaseCollectionController(
    ICollectionViewModelService collectionViewModelService,
    ICollectionService collectionService,
    IStoreService storeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IPictureViewModelService pictureViewModelService,
    IProductService productService,
    IAdminDataScope<Collection> scope)
    : BaseController
{
    /// <summary>Hook for host-specific UI-copy warnings that aren't access-scope decisions.
    /// Overridden by the Store subclass (Task 3); no-op everywhere else. Mirrors
    /// BaseCategoryController.EditWarningCheck.</summary>
    protected virtual void EditWarningCheck(Collection collection) { }

    // Exposed for host subclasses: primary-constructor parameters are not visible to derived
    // classes by name in C#, so Store's EditWarningCheck override needs this.
    protected ITranslationService TranslationService => translationService;
    protected IAdminDataScope<Collection> Scope => scope;

    #region List

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        var model = new CollectionListModel();
        // Admin only: Store never had this dropdown (it's implicitly single-store).
        // ShowStoreSelector can't gate this - it's true on both Global and Store scopes.
        if (scope.DefaultStoreId is null)
        {
            model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
        }

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command, CollectionListModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;
        var collections = await collectionService.GetAllCollections(model.SearchCollectionName,
            model.SearchStoreId, command.Page - 1, command.PageSize, true);
        var gridModel = new DataSourceResult {
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
        await AddLocales(languageService, model.Locales);
        await collectionViewModelService.PrepareLayoutsModel(model);
        await collectionViewModelService.PrepareDiscountModel(model, null, true);
        model.PageSize = catalogSettings.DefaultPageSize;
        model.PageSizeOptions = catalogSettings.DefaultPageSizeOptions;
        model.Published = true;
        model.AllowCustomersToSelectPageSize = true;
        collectionViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(CollectionModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var collection = await collectionViewModelService.InsertCollectionModel(model);
            Success(translationService.GetResource("Admin.Catalog.Collections.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = collection.Id }) : RedirectToAction("List");
        }

        await collectionViewModelService.PrepareLayoutsModel(model);
        await collectionViewModelService.PrepareDiscountModel(model, null, true);
        collectionViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var collection = await collectionService.GetCollectionById(id);
        if (collection == null) return RedirectToAction("List");

        EditWarningCheck(collection);
        // CanView, not HasAccess: viewing a shared/global collection is allowed on Store (with a
        // warning from EditWarningCheck above); only mutating one is restricted to the exclusive
        // single-store owner. See IAdminDataScope<TEntity>.CanView's doc comment.
        if (!await scope.CanView(collection)) return RedirectToAction("List");

        var model = collection.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = collection.GetTranslation(x => x.Name, languageId, false);
            locale.Description = collection.GetTranslation(x => x.Description, languageId, false);
            locale.BottomDescription = collection.GetTranslation(x => x.BottomDescription, languageId, false);
            locale.MetaKeywords = collection.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = collection.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = collection.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = collection.GetSeName(languageId, false);
        });
        await collectionViewModelService.PrepareLayoutsModel(model);
        await collectionViewModelService.PrepareDiscountModel(model, collection, false);
        collectionViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(CollectionModel model, bool continueEditing)
    {
        var collection = await collectionService.GetCollectionById(model.Id);
        if (collection == null) return RedirectToAction("List");
        if (!await scope.HasAccess(collection)) return RedirectToAction("Edit", new { id = collection.Id });

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            collection = await collectionViewModelService.UpdateCollectionModel(collection, model);
            Success(translationService.GetResource("Admin.Catalog.Collections.Updated"));

            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = collection.Id });
            }
            return RedirectToAction("List");
        }

        await collectionViewModelService.PrepareLayoutsModel(model);
        await collectionViewModelService.PrepareDiscountModel(model, collection, true);
        collectionViewModelService.PrepareSortOptionsModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var collection = await collectionService.GetCollectionById(id);
        if (collection == null) return RedirectToAction("List");
        if (!await scope.HasAccess(collection)) return RedirectToAction("Edit", new { id = collection.Id });

        if (ModelState.IsValid)
        {
            await collectionViewModelService.DeleteCollection(collection);
            Success(translationService.GetResource("Admin.Catalog.Collections.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = collection.Id });
    }

    #endregion

    #region Picture

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> PicturePopup(string collectionId)
    {
        var collection = await collectionService.GetCollectionById(collectionId);
        if (collection == null) return Content("Collection not exist");
        if (!await scope.HasAccess(collection)) return Content("This is not your collection");
        if (string.IsNullOrEmpty(collection.PictureId)) return Content("Picture not exist");

        return View("Partials/PicturePopup",
            await pictureViewModelService.PreparePictureModel(collection.PictureId, collection.Id));
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PicturePopup(PictureModel model)
    {
        if (ModelState.IsValid)
        {
            var collection = await collectionService.GetCollectionById(model.ObjectId);
            if (collection == null)
                throw new ArgumentException("No collection found with the specified id");
            if (!await scope.HasAccess(collection)) return Content("This is not your collection");
            if (string.IsNullOrEmpty(collection.PictureId))
                throw new ArgumentException("No picture found with the specified id");
            if (collection.PictureId != model.Id)
                throw new ArgumentException("Picture ident doesn't fit with collection");

            await pictureViewModelService.UpdatePicture(model);
            return Content("");
        }

        Error(ModelState);
        return View("Partials/PicturePopup", model);
    }

    #endregion
}
