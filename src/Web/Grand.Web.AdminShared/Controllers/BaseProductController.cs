using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Web.AdminShared.Controllers;

// Resource-key-prefix audit (2026-08-16, ARCH-001 Phase 1 Task 6, corrected after review — see Task 6's
// ledger entry). Inlined in full here (not just referenced) since planning artifacts under .superpowers/
// are untracked and do not survive in the repo once this branch merges.
//
// Templated via {scope.ResourceKeyPrefix} (Admin.<suffix> and Vendor.<suffix> both exist) — 22:
//   Common.All, Customers.Guest, Configuration.Tax.Settings.TaxCategories.None,
//   Catalog.Products.Added, Catalog.Products.Updated, Catalog.Products.Deleted,
//   Catalog.Products.Fields.ChangedWarning, Catalog.Products.Fields.DeliveryDate.None,
//   Catalog.Products.Fields.Warehouse.None, Catalog.Products.Bids.CantDeleteWithOrder,
//   Catalog.Products.List.SkuNotFound, Catalog.Products.List.SearchPublished.All,
//   Catalog.Products.List.SearchPublished.PublishedOnly, Catalog.Products.List.SearchPublished.UnpublishedOnly,
//   Catalog.Products.List.SearchPublished.MarkAsNew, Catalog.ProductReservations.CantDeleteWithOrder,
//   Catalog.Products.Calendar.CannotChangeInterval,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.MinLength,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.MaxLength,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileAllowedExtensions,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileMaximumSize,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.DefaultValue.
//
// Admin-only literal (no Vendor equivalent call site; keep as literal "Admin.<suffix>") — 6:
//   Catalog.Products.Permissions (Vendor has no Permissions-suffixed resource lookup anywhere - its
//     permission-denied paths don't emit this message), Catalog.Products.List.SearchPublished.ShowOnHomePage,
//   Catalog.Products.Imported, Catalog.Products.TierPrices.Fields.CustomerGroup.All,
//   Catalog.Products.TierPrices.Fields.Store.All, Common.UploadFile.
//
// Host-specific, not templated — 0: none found; every "Vendor.<suffix>" call site has a matching
//   "Admin.<suffix>" one, so nothing needs a scope.ResourceKeyPrefix == "Vendor" guard instead of templating.
//
// Store makes no separate resource lookups at all - every Store call site uses the literal "Admin.*" key
// directly (Store has no distinct resource set), consistent with StoreAdminDataScope.ResourceKeyPrefix
// returning "Admin".

[PermissionAuthorize(PermissionSystemName.Products)]
public abstract class BaseProductController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IInventoryManageService inventoryManageService,
    ILanguageService languageService,
    ITranslationService translationService,
    IProductReservationService productReservationService,
    IAuctionService auctionService,
    IDateTimeService dateTimeService,
    IPermissionService permissionService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    /// <summary>Hook for host-specific UI-copy warnings that aren't access-scope decisions.
    /// Overridden by the Store subclass; no-op everywhere else.</summary>
    protected virtual void EditWarningCheck(Product product) { }

    #region Product list / create / edit / delete

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        var model = await productViewModelService.PrepareProductListModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ProductList(DataSourceRequest command, ProductListModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;

        var (productModels, totalCount) =
            await productViewModelService.PrepareProductsModel(model, command.Page, command.PageSize);
        return Json(new DataSourceResult { Data = productModels.ToList(), Total = totalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToSku(ProductListModel model)
    {
        var product = await productService.GetProductBySku(model.GoDirectlyToSku);
        if (product != null)
        {
            // TODO(ARCH-001-followup): Store's pre-refactor code had a security-relevant bug here -
            // on access denial it fell into `if (!CanAccessProduct(product)) return RedirectToAction("Edit", ...)`,
            // i.e. it redirected an unauthorized caller straight to the Edit screen of a product outside
            // their store. (On access *granted* the old code had a separate, non-security bug: it fell
            // through to the "not found" Warning + redirect-to-List below instead of going to Edit.)
            // The merged behavior below deliberately tightens this: deny -> List (matching Vendor's
            // stricter pattern), grant -> Edit. This is an intentional behavior change, not a faithful
            // port - call it out in the PR description.
            if (!await scope.HasAccess(product))
                return RedirectToAction("List", "Product");
            return RedirectToAction("Edit", "Product", new { id = product.Id });
        }

        Warning(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SkuNotFound"));
        return RedirectToAction("List", "Product");
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new ProductModel { StoreId = scope.DefaultStoreId };
        await productViewModelService.PrepareProductModel(model, null, true, true);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(ProductModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null)
            {
                model.Stores = [scope.DefaultStoreId];
                model.StoreId = scope.DefaultStoreId;
            }

            var product = await productViewModelService.InsertProductModel(model);
            Success(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
        }

        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        await productViewModelService.PrepareProductModel(model, null, false, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var product = await productService.GetProductById(id, true);
        if (product == null) return RedirectToAction("List");

        EditWarningCheck(product);
        // CanView, not HasAccess: viewing a shared/global product is allowed on Store (with a warning
        // from EditWarningCheck above); only mutating one is restricted to the exclusive single-store
        // owner. See IAdminDataScope<TEntity>.CanView's doc comment and StoreAdminDataScope.CanView.
        if (!await scope.CanView(product)) return RedirectToAction("List");

        var model = product.ToModel(dateTimeService);
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        await productViewModelService.PrepareProductModel(model, product, false, false);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = product.GetTranslation(x => x.Name, languageId, false);
            locale.ShortDescription = product.GetTranslation(x => x.ShortDescription, languageId, false);
            locale.FullDescription = product.GetTranslation(x => x.FullDescription, languageId, false);
            locale.MetaKeywords = product.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = product.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = product.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = product.GetSeName(languageId, false);
        });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(ProductModel model, bool continueEditing)
    {
        var product = await productService.GetProductById(model.Id, true);
        if (product == null) return RedirectToAction("List");
        if (!await scope.HasAccess(product)) return RedirectToAction("Edit", new { id = product.Id });

        if (model.Ticks != product.Ticks)
        {
            Error(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Fields.ChangedWarning"));
            return RedirectToAction("Edit", new { id = product.Id });
        }

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null)
            {
                model.Stores = [scope.DefaultStoreId];
                model.StoreId = scope.DefaultStoreId;
            }

            product = await productViewModelService.UpdateProductModel(product, model);
            Success(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = product.Id });
            }

            return RedirectToAction("List");
        }

        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        await productViewModelService.PrepareProductModel(model, product, false, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var product = await productService.GetProductById(id, true);
        if (product == null) return RedirectToAction("List");
        if (!await scope.HasAccess(product)) return RedirectToAction("Edit", new { id });

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteProduct(product);
            Success(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(ICollection<string> selectedIds)
    {
        if (selectedIds == null || selectedIds.Count == 0) return Json(new { Result = true });

        // This is a mutation (bulk delete), so it uses the strict HasAccess, matching Edit(POST)/Delete
        // above — not a no-op pass-through to the service. Without this filter, Store gains an
        // unscoped bulk-delete endpoint (any store staff could delete any product id in the system,
        // bypassing AccessToEntityByStore entirely) purely because MVC routes actions regardless of
        // whether a host's views ever link to them.
        var products = await productService.GetProductsByIds(selectedIds.ToArray(), true);
        var allowedIds = new List<string>();
        foreach (var product in products)
            if (await scope.HasAccess(product))
                allowedIds.Add(product.Id);

        if (allowedIds.Count > 0) await productViewModelService.DeleteSelected(allowedIds);
        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    public async Task<IActionResult> CopyProduct(ProductModel model,
        [FromServices] ICopyProductService copyProductService, [FromServices] IPictureService pictureService)
    {
        var copyModel = model.CopyProductModel;
        try
        {
            var originalProduct = await productService.GetProductById(copyModel.Id, true);
            // CanView, not HasAccess: Store's original CopyProduct denies only when LimitedToStores is
            // true AND the staff member's store isn't among them — the same looser rule as Edit(GET)
            // above, not the strict mutation rule. See IAdminDataScope<TEntity>.CanView.
            if (!await scope.CanView(originalProduct)) return RedirectToAction("List");

            if (scope.DefaultStoreId is not null)
            {
                originalProduct.LimitedToStores = true;
                originalProduct.Stores.Clear();
                originalProduct.Stores.Add(scope.DefaultStoreId);
            }

            var newProduct = await copyProductService.CopyProduct(originalProduct, copyModel.Name, copyModel.Published);
            if (copyModel.CopyImages) await CopyImages(originalProduct, newProduct, pictureService);

            Success("The product has been copied successfully");
            return RedirectToAction("Edit", new { id = newProduct.Id });
        }
        catch (Exception exc)
        {
            Error(exc.Message);
            return RedirectToAction("Edit", new { id = copyModel.Id });
        }
    }

    private async Task CopyImages(Product originalProduct, Product newProduct, IPictureService pictureService)
    {
        foreach (var productPicture in originalProduct.ProductPictures)
        {
            var picture = await pictureService.GetPictureById(productPicture.PictureId);
            var pictureCopy = await pictureService.InsertPicture(
                await pictureService.LoadPictureBinary(picture),
                picture.MimeType,
                pictureService.GetPictureSeName(newProduct.Name),
                picture.AltAttribute,
                picture.TitleAttribute,
                false,
                Reference.Product,
                newProduct.Id);

            await productService.InsertProductPicture(new ProductPicture {
                PictureId = pictureCopy.Id,
                DisplayOrder = productPicture.DisplayOrder,
                IsDefault = productPicture.IsDefault
            }, newProduct.Id);
        }
    }

    #endregion

    #region Required products

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> LoadProductFriendlyNames(string productIds)
    {
        var result = "";

        if (!string.IsNullOrWhiteSpace(productIds))
        {
            var ids = productIds
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var products = await productService.GetProductsByIds(ids.ToArray(), true);
            for (var i = 0; i <= products.Count - 1; i++)
            {
                // Filters the friendly-name list, not a hard deny of the whole action: matches Store's
                // CanAccessProduct loop and Vendor's HasAccessToProduct loop, both of which skip
                // inaccessible products silently rather than erroring the whole request. Both are the
                // strict rule (AccessToEntityByStore / VendorId equality), so HasAccess (not CanView) is
                // correct here - this is filtering a display list, not opening/copying a single entity.
                if (!await scope.HasAccess(products[i])) continue;

                result += products[i].Name;
                if (i != products.Count - 1)
                    result += ", ";
            }
        }

        return Json(new { Text = result });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RequiredProductAddPopup(string productIdsInput)
    {
        // scope.DefaultStoreId already encodes the per-host default exactly: null for Admin (global) and
        // Vendor (not store-scoped), StaffStoreId for Store - matching Store's original
        // PrepareAddRequiredProductModel(StaffStoreId) call and Admin/Vendor's parameterless call.
        var model = await productViewModelService.PrepareAddRequiredProductModel(scope.DefaultStoreId ?? "");
        // Unused by any of the three views (all three read productIdsInput straight off the query string
        // via Context.Request.Query, not ViewBag), but Admin and Vendor both set it and Store silently
        // drops its own parameter - kept here for parity; it is inert either way.
        ViewBag.productIdsInput = productIdsInput;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RequiredProductAddPopupList(DataSourceRequest command,
        ProductModel.AddRequiredProductModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;

        var (products, totalCount) =
            await productViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion
}
