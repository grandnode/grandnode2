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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Web.AdminShared.Controllers;

// Resource-key-prefix audit (2026-08-16, ARCH-001 Phase 1 Task 6, corrected after review — see Task 6's
// ledger entry). Inlined in full here (not just referenced) since planning artifacts under .superpowers/
// are untracked and do not survive in the repo once this branch merges.
//
// Templated via {scope.ResourceKeyPrefix} (Admin.<suffix> and Vendor.<suffix> both exist) — 23:
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
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.DefaultValue,
//   Catalog.Products.Permissions (CORRECTED 2026-08-16, Task 8 row "Product categories": this row's
//     original pass kept it as an "Admin-only literal" below, trusting Task 6's audit - but that audit
//     only scanned the 5 files under migration [2 ProductControllers + 2 ProductViewModelServices], never
//     validators. "Vendor.Catalog.Products.Permissions" genuinely exists in
//     src/Web/Grand.Web/App_Data/Resources/Upgrade/en_220.xml and is consumed by
//     Grand.Web.Vendor/Validators/Catalog/ProductValidVendor.cs and BundleProductModelValidator.cs.
//     Lesson for later rows: "no call site found in the files under migration" is NOT the same claim as
//     "no resource key exists for Vendor" - check the XML resource files too before treating a key as
//     host-specific).
//
// Admin-only literal (no Vendor equivalent call site; keep as literal "Admin.<suffix>") — 5:
//   Catalog.Products.List.SearchPublished.ShowOnHomePage,
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

    #region Product categories

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict), not CanView: mirrors Store's CanAccessProduct (AccessToEntityByStore) and
        // Vendor's CheckAccessToProduct (VendorId equality) - both strict rules, both gate this same
        // action on their respective hosts. Applying it uniformly also closes a real gap: Vendor's
        // original ProductCategoryInsert/Update/Delete (below) had no ownership check at all, letting
        // any vendor mutate another vendor's product-category mappings by id.
        if (!await scope.HasAccess(product))
            // Templated, not the literal "Admin.Catalog.Products.Permissions": Task 6's audit (the header
            // comment above) only covered the files under migration (the 2 ProductControllers + 2
            // ProductViewModelServices) and found no "Permissions"-suffixed GetResource call in Vendor's
            // copies of *those* files - but "Vendor.Catalog.Products.Permissions" genuinely exists at the
            // XML resource layer (src/Web/Grand.Web/App_Data/Resources/Upgrade/en_220.xml, consumed by
            // Grand.Web.Vendor's ProductValidVendor/BundleProductModelValidator). That audit's scope was
            // narrower than "Admin-only" - don't cite it as precedent for skipping templating elsewhere.
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productCategoriesModel = await productViewModelService.PrepareProductCategoryModel(product);
        var gridModel = new DataSourceResult {
            Data = productCategoriesModel,
            Total = productCategoriesModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryInsert(ProductModel.ProductCategoryModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
            try
            {
                await productViewModelService.InsertProductCategoryModel(model);
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryUpdate(ProductModel.ProductCategoryModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
            try
            {
                await productViewModelService.UpdateProductCategoryModel(model);
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryDelete(ProductModel.ProductCategoryModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteProductCategory(model.Id, model.ProductId);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Product collections

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict), not CanView: same shape as "Product categories" above - mirrors Store's
        // CanAccessProduct and Vendor's CheckAccessToProduct gating this action on both hosts. Applying
        // it uniformly also closes the same kind of gap found in "Product categories": Store's and
        // Vendor's original ProductCollectionInsert/Update/Delete (below) had no ownership check at all
        // - only List checked - letting any store manager or vendor mutate another party's
        // product-collection mappings by id.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productCollectionsModel = await productViewModelService.PrepareProductCollectionModel(product);
        var gridModel = new DataSourceResult {
            Data = productCollectionsModel,
            Total = productCollectionsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionInsert(ProductModel.ProductCollectionModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
            try
            {
                await productViewModelService.InsertProductCollection(model);
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionUpdate(ProductModel.ProductCollectionModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
            try
            {
                await productViewModelService.UpdateProductCollection(model);
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionDelete(ProductModel.ProductCollectionModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteProductCollection(model.Id, model.ProductId);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Related products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict), not CanView: same shape as "Product categories"/"Product collections" -
        // mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating this action on both
        // hosts. Applying it uniformly also closes the same kind of gap found in those two regions:
        // Vendor's original RelatedProductUpdate/Delete/AddPopup(POST) (below) had no ownership check at
        // all - only List checked - letting any vendor mutate another vendor's related-product mappings
        // by id.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var relatedProducts = product.RelatedProducts.OrderBy(x => x.DisplayOrder);
        var relatedProductsModel = new List<ProductModel.RelatedProductModel>();
        foreach (var x in relatedProducts)
            relatedProductsModel.Add(new ProductModel.RelatedProductModel {
                Id = x.Id,
                ProductId1 = productId,
                ProductId2 = x.ProductId2,
                Product2Name = (await productService.GetProductById(x.ProductId2))?.Name,
                DisplayOrder = x.DisplayOrder
            });

        var gridModel = new DataSourceResult {
            Data = relatedProductsModel,
            Total = relatedProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductUpdate(ProductModel.RelatedProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId1);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.UpdateRelatedProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductDelete(ProductModel.RelatedProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId1);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteRelatedProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RelatedProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. scope.DefaultStoreId ?? "" matches Store's
        // PrepareRelatedProductModel(StaffStoreId) call and Admin/Vendor's parameterless call.
        var model = await productViewModelService.PrepareRelatedProductModel(scope.DefaultStoreId ?? "");
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductAddPopupList(DataSourceRequest command,
        ProductModel.AddRelatedProductModel model)
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductAddPopup(ProductModel.AddRelatedProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // RelatedProductAddPopup(POST) had no check at all, letting any vendor add related-product
        // mappings onto another vendor's product by posting its id - closed here the same way as the
        // List/Update/Delete gap above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await productViewModelService.InsertRelatedProductModel(model);
            return Content("");
        }

        return await InvalidRelatedProductAddPopupResult(model);
    }

    /// <summary>Hook for the host-specific invalid-model-state response of the AddPopup(POST) action
    /// above. Admin and Store both re-prepare the popup model and return the View; Vendor instead
    /// returns Content(ModelState.GetErrors()) - a Vendor-only extension method that AdminShared cannot
    /// reference. Default here matches Admin/Store; a future Vendor subclass overrides it once hosts are
    /// subclassed onto BaseProductController (Task 11).</summary>
    protected virtual async Task<IActionResult> InvalidRelatedProductAddPopupResult(ProductModel.AddRelatedProductModel model)
    {
        Error(ModelState);
        model = await productViewModelService.PrepareRelatedProductModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    #endregion

    #region Similar products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict), not CanView: same shape as "Related products" above - mirrors Store's
        // CanAccessProduct check on this action. Applying it uniformly also closes a real gap: Vendor's
        // original SimilarProductUpdate/Delete/AddPopup(GET/POST) (below) had no ownership check at all -
        // only List checked (via CheckAccessToProduct) - letting any vendor mutate another vendor's
        // similar-product mappings by id.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var similarProducts = product.SimilarProducts.OrderBy(x => x.DisplayOrder);
        var similarProductsModel = new List<ProductModel.SimilarProductModel>();
        foreach (var x in similarProducts)
            similarProductsModel.Add(new ProductModel.SimilarProductModel {
                Id = x.Id,
                ProductId1 = productId,
                ProductId2 = x.ProductId2,
                Product2Name = (await productService.GetProductById(x.ProductId2))?.Name,
                DisplayOrder = x.DisplayOrder
            });

        var gridModel = new DataSourceResult {
            Data = similarProductsModel,
            Total = similarProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductUpdate(ProductModel.SimilarProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId1);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.UpdateSimilarProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductDelete(ProductModel.SimilarProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId1);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteSimilarProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SimilarProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. scope.DefaultStoreId ?? "" matches Store's
        // PrepareSimilarProductModel(StaffStoreId) call and Admin/Vendor's parameterless call.
        var model = await productViewModelService.PrepareSimilarProductModel(scope.DefaultStoreId ?? "");
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductAddPopupList(DataSourceRequest command,
        ProductModel.AddSimilarProductModel model)
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductAddPopup(ProductModel.AddSimilarProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // SimilarProductAddPopup(POST) had no check at all, letting any vendor add similar-product
        // mappings onto another vendor's product by posting its id - closed here the same way as the
        // List/Update/Delete gap above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await productViewModelService.InsertSimilarProductModel(model);
            return Content("");
        }

        return await InvalidSimilarProductAddPopupResult(model);
    }

    /// <summary>Hook for the host-specific invalid-model-state response of the AddPopup(POST) action
    /// above. Admin and Store both re-prepare the popup model and return the View; Vendor instead
    /// returns Content(ModelState.GetErrors()) - a Vendor-only extension method that AdminShared cannot
    /// reference. Default here matches Admin/Store; a future Vendor subclass overrides it once hosts are
    /// subclassed onto BaseProductController (Task 11).</summary>
    protected virtual async Task<IActionResult> InvalidSimilarProductAddPopupResult(ProductModel.AddSimilarProductModel model)
    {
        Error(ModelState);
        model = await productViewModelService.PrepareSimilarProductModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    #endregion

    #region Bundle products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> BundleProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict), not CanView: same shape as "Related products"/"Similar products" above -
        // mirrors Store's CanAccessProduct check on this action. Applying it uniformly also closes the
        // same kind of gap found in those two regions: Vendor's original BundleProductUpdate/Delete/
        // AddPopup(GET/POST) (below) had no ownership check at all - only List checked (via
        // CheckAccessToProduct) - letting any vendor mutate another vendor's bundle-product mappings by id.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var bundleProducts = product.BundleProducts.OrderBy(x => x.DisplayOrder);
        var bundleProductsModel = new List<ProductModel.BundleProductModel>();
        foreach (var x in bundleProducts)
            bundleProductsModel.Add(new ProductModel.BundleProductModel {
                Id = x.Id,
                ProductBundleId = productId,
                ProductId = x.ProductId,
                ProductName = (await productService.GetProductById(x.ProductId))?.Name,
                DisplayOrder = x.DisplayOrder,
                Quantity = x.Quantity
            });
        var gridModel = new DataSourceResult {
            Data = bundleProductsModel,
            Total = bundleProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BundleProductUpdate(ProductModel.BundleProductModel model)
    {
        var product = await productService.GetProductById(model.ProductBundleId);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.UpdateBundleProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BundleProductDelete(ProductModel.BundleProductModel model)
    {
        var product = await productService.GetProductById(model.ProductBundleId);
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteBundleProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> BundleProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. scope.DefaultStoreId ?? "" matches Store's
        // PrepareBundleProductModel(StaffStoreId) call and Admin/Vendor's parameterless call.
        var model = await productViewModelService.PrepareBundleProductModel(scope.DefaultStoreId ?? "");
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BundleProductAddPopupList(DataSourceRequest command,
        ProductModel.AddBundleProductModel model)
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BundleProductAddPopup(ProductModel.AddBundleProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // BundleProductAddPopup(POST) had no check at all, letting any vendor add bundle-product mappings
        // onto another vendor's product by posting its id - closed here the same way as the
        // List/Update/Delete gap above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await productViewModelService.InsertBundleProductModel(model);
            return Content("");
        }

        return await InvalidBundleProductAddPopupResult(model);
    }

    /// <summary>Hook for the host-specific invalid-model-state response of the AddPopup(POST) action
    /// above. Admin and Store both re-prepare the popup model and return the View; Vendor instead
    /// returns Content(ModelState.GetErrors()) - a Vendor-only extension method that AdminShared cannot
    /// reference. Default here matches Admin/Store; a future Vendor subclass overrides it once hosts are
    /// subclassed onto BaseProductController (Task 11).</summary>
    protected virtual async Task<IActionResult> InvalidBundleProductAddPopupResult(ProductModel.AddBundleProductModel model)
    {
        Error(ModelState);
        model = await productViewModelService.PrepareBundleProductModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    #endregion

    #region Cross-sell products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict), not CanView: same shape as "Related products"/"Bundle products" above -
        // mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating this action on both
        // hosts. Admin's original CrossSellProductList had no check at all - applying HasAccess uniformly
        // also closes that gap without changing Admin's superuser behaviour (HasAccess is a no-op for
        // Admin's scope).
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var crossSellProducts = product.CrossSellProduct;
        var crossSellProductsModel = new List<ProductModel.CrossSellProductModel>();
        foreach (var x in crossSellProducts)
            crossSellProductsModel.Add(new ProductModel.CrossSellProductModel {
                Id = x,
                ProductId = product.Id,
                Product2Name = (await productService.GetProductById(x))?.Name
            });
        var gridModel = new DataSourceResult {
            Data = crossSellProductsModel,
            Total = crossSellProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductDelete(ProductModel.CrossSellProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null) throw new ArgumentException("Product not exists");

        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // CrossSellProductDelete had no check at all, letting any vendor delete another vendor's
        // cross-sell-product mappings by id - closed here the same way as the List gap above.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var crossSellProduct = product.CrossSellProduct.FirstOrDefault(x => x == model.Id);
        if (string.IsNullOrEmpty(crossSellProduct))
            throw new ArgumentException("No cross-sell product found with the specified id");

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteCrossSellProduct(product.Id, crossSellProduct);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CrossSellProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. scope.DefaultStoreId ?? "" matches Store's
        // PrepareCrossSellProductModel(StaffStoreId) call and Admin/Vendor's parameterless call.
        var model = await productViewModelService.PrepareCrossSellProductModel(scope.DefaultStoreId ?? "");
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductAddPopupList(DataSourceRequest command,
        ProductModel.AddCrossSellProductModel model)
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductAddPopup(ProductModel.AddCrossSellProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // CrossSellProductAddPopup(POST) had no check at all, letting any vendor add cross-sell-product
        // mappings onto another vendor's product by posting its id - closed here the same way as the
        // List/Delete gap above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await productViewModelService.InsertCrossSellProductModel(model);
            return Content("");
        }

        return await InvalidCrossSellProductAddPopupResult(model);
    }

    /// <summary>Hook for the host-specific invalid-model-state response of the AddPopup(POST) action
    /// above. Admin and Store both re-prepare the popup model and return the View; Vendor instead
    /// returns Content(ModelState.GetErrors()) - a Vendor-only extension method that AdminShared cannot
    /// reference. Default here matches Admin/Store; a future Vendor subclass overrides it once hosts are
    /// subclassed onto BaseProductController (Task 11).</summary>
    protected virtual async Task<IActionResult> InvalidCrossSellProductAddPopupResult(ProductModel.AddCrossSellProductModel model)
    {
        Error(ModelState);
        model = await productViewModelService.PrepareCrossSellProductModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    #endregion

    #region Recommended products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict), not CanView: same shape as "Cross-sell products" above - mirrors Store's
        // CanAccessProduct and Vendor's CheckAccessToProduct gating this action on both hosts. Admin's
        // original RecommendedProductList had no check at all - applying HasAccess uniformly also closes
        // that gap without changing Admin's superuser behaviour (HasAccess is a no-op for Admin's scope).
        // Vendor's original signature also dropped the DataSourceRequest command parameter entirely
        // (unused by the body on any host either way) - kept here for parity with Admin/Store.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var recommendedProductsModel = new List<ProductModel.RecommendedProductModel>();
        foreach (var x in product.RecommendedProduct)
            recommendedProductsModel.Add(new ProductModel.RecommendedProductModel {
                Id = x,
                ProductId = product.Id,
                Product2Name = (await productService.GetProductById(x))?.Name
            });
        var gridModel = new DataSourceResult {
            Data = recommendedProductsModel,
            Total = recommendedProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductDelete(ProductModel.RecommendedProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null) throw new ArgumentException("Product not exists");

        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // RecommendedProductDelete had no check at all, letting any vendor delete another vendor's
        // recommended-product mappings by id - closed here the same way as the List gap above.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var recommendedProduct = product.RecommendedProduct.FirstOrDefault(x => x == model.Id);
        if (string.IsNullOrEmpty(recommendedProduct))
            throw new ArgumentException("No recommended product found with the specified id");

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteRecommendedProduct(product.Id, recommendedProduct);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RecommendedProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. scope.DefaultStoreId ?? "" matches Store's
        // PrepareRecommendedProductModel(StaffStoreId) call and Admin/Vendor's parameterless call.
        var model = await productViewModelService.PrepareRecommendedProductModel(scope.DefaultStoreId ?? "");
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductAddPopupList(DataSourceRequest command,
        ProductModel.AddRecommendedProductModel model)
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductAddPopup(ProductModel.AddRecommendedProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // RecommendedProductAddPopup(POST) had no check at all, letting any vendor add recommended-product
        // mappings onto another vendor's product by posting its id - closed here the same way as the
        // List/Delete gap above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            if (model.SelectedProductIds != null) await productViewModelService.InsertRecommendedProductModel(model);
            return Content("");
        }

        return await InvalidRecommendedProductAddPopupResult(model);
    }

    /// <summary>Hook for the host-specific invalid-model-state response of the AddPopup(POST) action
    /// above. Admin and Store both re-prepare the popup model and return the View; Vendor instead
    /// returns Content(ModelState.GetErrors()) - a Vendor-only extension method that AdminShared cannot
    /// reference. Default here matches Admin/Store; a future Vendor subclass overrides it once hosts are
    /// subclassed onto BaseProductController (Task 11).</summary>
    protected virtual async Task<IActionResult> InvalidRecommendedProductAddPopupResult(ProductModel.AddRecommendedProductModel model)
    {
        Error(ModelState);
        model = await productViewModelService.PrepareRecommendedProductModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    #endregion

    #region Associated products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original had no check at all - GlobalAdminDataScope.HasAccess
        // is a no-op there, so this closes that gap the same way as every other row in this task.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        // AssociatedProductVendorId (hook below): Vendor's original also passed CurrentVendor.Id into
        // GetAssociatedProducts(vendorId:) so a vendor only sees, among a grouped product's full
        // associated-product set, the ones they themselves own. That vendor filter is unconditional in
        // GetAssociatedProducts regardless of showHidden, unlike its storeId parameter (which only applies
        // when showHidden is false - moot here since all three hosts pass showHidden: true).
        var associatedProducts = await productService.GetAssociatedProducts(productId,
            vendorId: AssociatedProductVendorId, showHidden: true);
        var associatedProductsModel = associatedProducts
            .Select(x => new ProductModel.AssociatedProductModel {
                Id = x.Id,
                ProductId = productId,
                ProductName = x.Name,
                DisplayOrder = x.DisplayOrder
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = associatedProductsModel,
            Total = associatedProductsModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductUpdate(ProductModel.AssociatedProductModel model)
    {
        var associatedProduct = await productService.GetProductById(model.Id);
        if (associatedProduct == null)
            throw new ArgumentException("No associated product found with the specified id");

        // HasAccess (strict): mirrors Vendor's inline HasAccessToProduct(associatedProduct) check and
        // Store's CanAccessProduct. Admin's original had no check at all.
        if (!await scope.HasAccess(associatedProduct))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            associatedProduct.DisplayOrder = model.DisplayOrder;
            await productService.UpdateAssociatedProduct(associatedProduct);

            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductDelete(ProductModel.AssociatedProductModel model)
    {
        var product = await productService.GetProductById(model.Id);
        if (product == null)
            throw new ArgumentException("No associated product found with the specified id");

        // HasAccess (strict): mirrors Vendor's inline HasAccessToProduct(product) check and Store's
        // CanAccessProduct. Admin's original had no check at all.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteAssociatedProduct(product);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AssociatedProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (all open this popup unconditionally
        // once the Edit permission is satisfied) - only the mutating actions below tie access to a
        // specific product. scope.DefaultStoreId ?? "" matches Store's
        // PrepareAssociatedProductModel(StaffStoreId) call and Admin/Vendor's parameterless call.
        var model = await productViewModelService.PrepareAssociatedProductModel(scope.DefaultStoreId ?? "");
        model.ProductId = productId;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductAddPopupList(DataSourceRequest command,
        ProductModel.AddAssociatedProductModel model)
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductAddPopup(ProductModel.AddAssociatedProductModel model)
    {
        var parentProduct = await productService.GetProductById(model.ProductId);
        // HasAccess (strict): mirrors Store's CanAccessProduct(parentProduct) check on this action.
        // Vendor's controller had NO check on the parent product at all (see below for the selected-ids
        // side). Vendor's own service-layer InsertAssociatedProductModel already filtered each selected
        // product via HasAccessToProduct before reparenting it, but never checked the parent - so a vendor
        // could attach their own products under another vendor's grouped product (cross-vendor storefront
        // pollution / data-integrity issue, not a write to someone else's product record). Closed here the
        // same way as every other AddPopup(POST) in this task.
        if (!await scope.HasAccess(parentProduct))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            // InsertAssociatedProductModel reparents each selected product - it writes
            // ParentGroupedProductId directly onto that product's own record - unlike
            // Related/Similar/Bundle/Cross-sell/Recommended, whose Insert*ProductModel only adds a mapping
            // entry that references the selected product's id from the parent's own list; the selected
            // product's record is never itself modified there. So here, unlike those simpler regions,
            // every selected id must independently pass HasAccess too - the same per-id filtering Store's
            // controller-level original already did, and which Vendor's original also had, but only at the
            // service layer (Grand.Web.Vendor/Services/ProductViewModelService.cs InsertAssociatedProductModel:
            // `if (product == null || !HasAccessToProduct(product)) continue;`). BaseProductController
            // injects AdminShared's IProductViewModelService - the unfiltered variant - not Vendor's own, so
            // once Vendor is subclassed onto this base (Task 11) it loses that service-layer filter
            // entirely. Enforcing the per-id check here at the controller level is what preserves the
            // invariant going forward.
            if (model.SelectedProductIds != null)
            {
                var validIds = new List<string>();
                foreach (var id in model.SelectedProductIds)
                {
                    var selected = await productService.GetProductById(id);
                    if (await scope.HasAccess(selected)) validIds.Add(id);
                }

                model.SelectedProductIds = validIds.ToArray();
                if (validIds.Count > 0) await productViewModelService.InsertAssociatedProductModel(model);
            }

            return Content("");
        }

        // Unlike Related/Similar/Bundle/Cross-sell/Recommended, all three original hosts share the exact
        // same invalid-model-state handling here (Error(ModelState) + re-prepare + View) - Vendor's
        // AssociatedProductAddPopup(POST) does not use the Content(ModelState.GetErrors()) shortcut it
        // uses in those other regions, so no host-specific hook is needed for this action.
        Error(ModelState);
        model = await productViewModelService.PrepareAssociatedProductModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    /// <summary>Vendor id to filter the associated-products grid by, in addition to the HasAccess gate in
    /// AssociatedProductList above. Vendor's original passed CurrentVendor.Id into
    /// GetAssociatedProducts(vendorId:) so a vendor only sees the subset of a grouped product's associated
    /// products that they themselves own. Admin/Store passed no vendorId (both show every associated
    /// product on the parent). Empty here, matching Admin/Store; a future Vendor subclass overrides it
    /// once hosts are subclassed onto BaseProductController (Task 11).</summary>
    protected virtual string AssociatedProductVendorId => "";

    #endregion

    #region Product pictures

    [HttpPost]
    public async Task<IActionResult> ProductPictureAdd(
        IFormFileCollection files,
        Reference reference, string objectId,
        [FromServices] IPictureService pictureService,
        [FromServices] MediaSettings mediaSettings)
    {
        if (!await permissionService.Authorize(PermissionSystemName.Pictures))
            return Json(new {
                success = false,
                message = "Access denied - picture permissions"
            });

        if (reference != Reference.Product || string.IsNullOrEmpty(objectId))
            return Json(new {
                success = false,
                message = "Please save form before upload new pictures"
            });

        if (!files.Any())
            return Json(new {
                success = false,
                message = "No files uploaded"
            });

        var product = await productService.GetProductById(objectId);

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's inline
        // WorkContext.HasAccessToProduct check gating this action on both hosts. Admin's original had no
        // check at all - GlobalAdminDataScope.HasAccess is a no-op there, so this closes that gap the same
        // way as every other row in this task. Message text kept generic rather than reusing either host's
        // wording ("Access denied - staff permissions" / "Access denied - vendor permissions") since this
        // is a shared, host-neutral action now.
        if (!await scope.HasAccess(product))
            return Json(new {
                success = false,
                message = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions")
            });

        // File-upload validation note (ARCH-001 Phase 1 Task 8 row "Product pictures", 2026-08-16):
        // extension checking here already goes through FileExtensions.GetAllowedMediaFileTypes, which -
        // per commit a153496a6's fix - falls back to a safe image-only allow-list when
        // mediaSettings.AllowedFileTypes is empty, so the "empty config = any extension" bypass that
        // commit fixed for attribute uploads does not apply here. However, unlike the attribute-upload
        // paths that commit hardened (Contact/ShoppingCart/Product's ValidationFileMaximumSize check
        // against file.Length before buffering), this action has NO file-size limit at all in any of the
        // three original hosts - file.GetDownloadBits() buffers the full upload into memory unconditionally
        // for every file that passes the extension check. This is pre-existing, identical behavior across
        // all three hosts (not introduced by this consolidation), so it is ported as-is rather than
        // "fixed" here per this row's instructions - flagging as a concern: the admin/vendor/store picture
        // upload endpoints may be exposed to the same memory-DoS pattern a153496a6 fixed elsewhere, and
        // would need an explicit size check (and a decision on what setting should carry the limit, since
        // MediaSettings has no equivalent of ValidationFileMaximumSize) before that gap is closed.
        var values = new List<(string pictureUrl, string pictureId)>();
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.FileName);
            var contentType = file.ContentType;
            var fileExtension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(contentType))
                _ = new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);

            if (FileExtensions.GetAllowedMediaFileTypes(mediaSettings.AllowedFileTypes).IsAllowedMediaFileType(fileExtension))
            {
                var fileBinary = file.GetDownloadBits();
                //insert picture
                var picture = await pictureService.InsertPicture(fileBinary, contentType, null, reference: reference,
                    objectId: objectId);
                var pictureUrl = await pictureService.GetPictureUrl(picture);

                values.Add((pictureUrl, picture.Id));
                //assign picture to the product
                await productViewModelService.InsertProductPicture(product, picture, 0);
            }
        }

        return Json(new { success = values.Any(), data = values });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductPictureList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original had no check at all. Vendor's original signature
        // also lacked the unused `DataSourceRequest command` parameter that Admin/Store both bind (Kendo
        // posts it, but no host ever reads it) - kept here to match the two-of-three shape; harmless for
        // Vendor since an unused extra bound parameter changes nothing about the response.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productPicturesModel = await productViewModelService.PrepareProductPicturesModel(product);
        var gridModel = new DataSourceResult {
            Data = productPicturesModel,
            Total = productPicturesModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ProductPicturePopup(string productId, string id)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            return Content("Product not exist");

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original had no check at all.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var pp = product.ProductPictures.FirstOrDefault(x => x.Id == id);
        if (pp == null)
            return Content("Product picture not exist");

        var (model, picture) = await productViewModelService.PrepareProductPictureModel(product, pp);
        //locales
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.AltAttribute = picture?.GetTranslation(x => x.AltAttribute, languageId, false);
            locale.TitleAttribute = picture?.GetTranslation(x => x.TitleAttribute, languageId, false);
        });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPicturePopup(ProductModel.ProductPictureModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
            // ProductPicturePopup(POST) had no check at all, letting any vendor rename/re-alt-text another
            // vendor's product picture by posting its productId/model.Id - closed here the same way as the
            // other rows in this task.
            if (!await scope.HasAccess(product))
                throw new ArgumentException(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            if (product.ProductPictures.FirstOrDefault(x => x.Id == model.Id) == null)
                throw new ArgumentException("No product picture found with the specified id");

            await productViewModelService.UpdateProductPicture(model);

            return Content("");
        }

        Error(ModelState);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPictureDelete(ProductModel.ProductPictureModel model)
    {
        var product = await productService.GetProductById(model.ProductId);

        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
        // ProductPictureDelete had no check at all, letting any vendor delete another vendor's product
        // picture by posting its productId/model.Id - closed here the same way as ProductPicturePopup(POST)
        // above.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteProductPicture(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Product specification attributes

    //ajax
    [AcceptVerbs("GET")]
    public async Task<IActionResult> GetOptionsByAttributeId(string attributeId,
        [FromServices] ISpecificationAttributeService specificationAttributeService)
    {
        if (string.IsNullOrEmpty(attributeId))
            return Json("");

        var options =
            (await specificationAttributeService.GetSpecificationAttributeById(attributeId))
            .SpecificationAttributeOptions.OrderBy(x => x.DisplayOrder);
        var result = (from o in options
            select new { id = o.Id, name = o.Name }).ToList();
        return Json(result);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original ProductSpecAttrList had no check at all - applying
        // HasAccess uniformly also closes that gap without changing Admin's superuser behaviour (HasAccess
        // is a no-op for Admin's scope). Vendor's original signature also dropped the DataSourceRequest
        // command parameter entirely (unused by the body on any host either way) - kept here for parity
        // with Admin/Store.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productrSpecsModel = await productViewModelService.PrepareProductSpecificationAttributeModel(product);
        var gridModel = new DataSourceResult {
            Data = productrSpecsModel,
            Total = productrSpecsModel.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductSpecAttrPopup(
        [FromServices] ISpecificationAttributeService specificationAttributeService,
        string productId, string id)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct check on
        // this action. Admin's original ProductSpecAttrPopup(GET) had no check at all - closed the same
        // way as ProductSpecAttrList above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var model = new ProductModel.AddProductSpecificationAttributeModel {
            //default specs values
            ShowOnProductPage = true
        };

        if (!string.IsNullOrEmpty(id))
        {
            var specification = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == id);
            if (specification != null) model = specification.ToModel();
        }

        model.AvailableAttributes = await PrepareAvailableAttributes(specificationAttributeService);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrPopup(
        [FromServices] ISpecificationAttributeService specificationAttributeService,
        ProductModel.AddProductSpecificationAttributeModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId);
            if (product == null)
                return Content("Product not exists");

            // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
            // ProductSpecAttrPopup(POST) had no check at all, letting any vendor add/edit specification
            // attributes on another vendor's product by posting its id - closed here the same way as the
            // GET popup above. Vendor's original call also used a two-arg
            // UpdateProductSpecificationAttributeModel(psa, model) overload that does not exist on the
            // shared IProductViewModelService; the shared three-arg (product, psa, model) overload -
            // already used by Admin/Store - is used here instead.
            if (!await scope.HasAccess(product))
                return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == model.Id);
            if (psa == null)
                await productViewModelService.InsertProductSpecificationAttributeModel(model, product);
            else
                await productViewModelService.UpdateProductSpecificationAttributeModel(product, psa, model);

            return new JsonResult("");
        }

        Error(ModelState);
        model.AvailableAttributes = await PrepareAvailableAttributes(specificationAttributeService);

        return View(model);
    }

    /// <summary>scope.DefaultStoreId ?? "" matches Store's PrepareAvailableAttributes(StaffStoreId) call
    /// and Admin/Vendor's parameterless call (both pass an empty storeId, seeing every specification
    /// attribute regardless of store).</summary>
    private async Task<List<SelectListItem>> PrepareAvailableAttributes(
        ISpecificationAttributeService specificationAttributeService)
    {
        var availableSpecificationAttributes = new List<SelectListItem>();
        foreach (var sa in await specificationAttributeService.GetSpecificationAttributes(scope.DefaultStoreId ?? ""))
            availableSpecificationAttributes.Add(new SelectListItem {
                Text = sa.Name,
                Value = sa.Id
            });
        return availableSpecificationAttributes;
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrDelete(ProductSpecificationAttributeModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId);
            if (product == null)
                return Content("Product not exists");

            // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
            // ProductSpecAttrDelete had no check at all, letting any vendor delete another vendor's
            // specification attribute mapping by posting its productId/model.Id - closed here the same
            // way as ProductSpecAttrPopup(POST) above.
            if (!await scope.HasAccess(product))
                return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == model.Id);
            if (psa == null)
                throw new ArgumentException("No specification attribute found with the specified id");

            await productViewModelService.DeleteProductSpecificationAttribute(product, psa);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Purchased with order

    // Type note: this method covers Admin and Store only. Both already share
    // Grand.Web.AdminShared's IOrderViewModelService and Models.Orders.OrderListModel (see the usings
    // at the top of this file). Vendor defines its own, structurally different
    // Grand.Web.Vendor.Interfaces.IOrderViewModelService and Grand.Web.Vendor.Models.Orders.OrderListModel
    // (no StoreId property at all - Vendor's PrepareOrderModel scopes by
    // _contextAccessor.WorkContext.CurrentVendor.Id internally, not via any model field), so it cannot
    // bind to this signature. That vendor-id scoping lives entirely inside Vendor's own
    // OrderViewModelService, outside anything IAdminDataScope<Product> expresses - flagging as a concern
    // per the task brief rather than inventing a shared model/service pair. Not virtual: C#'s override
    // rules require an exact parameter-type match, so a Vendor override using
    // Grand.Web.Vendor.Interfaces.IOrderViewModelService/Models.Orders.OrderListModel could never compile
    // as an override of this signature anyway. When Vendor is wired onto this base controller (Task 11),
    // its subclass will declare its own PurchasedWithOrders action with `new` to shadow this one (a
    // standard, valid ASP.NET Core MVC pattern for a derived controller needing an incompatible signature
    // under the same action name), not override it.
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> PurchasedWithOrders(DataSourceRequest command, string productId,
        [FromServices] IOrderViewModelService orderViewModelService)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageOrders))
            return Json(new DataSourceResult {
                Data = null,
                Total = 0
            });

        var product = await productService.GetProductById(productId);

        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Admin's original had
        // no check at all - GlobalAdminDataScope.HasAccess is a no-op there, so this closes that gap the
        // same way as every other row in this task.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var model = new OrderListModel {
            ProductId = productId,
            // DefaultStoreId is the staff member's store for Store (matches its original
            // model.StoreId = StaffStoreId) and null for Admin (matches its original, which never set
            // StoreId at all).
            StoreId = scope.DefaultStoreId
        };

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = orderModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion

    #region Reviews

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> Reviews(DataSourceRequest command, string productId,
        [FromServices] IProductReviewService productReviewService)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original had no check at all - GlobalAdminDataScope.HasAccess
        // is a no-op there, so this closes that gap the same way as every other row in this task.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        // DefaultStoreId is the staff member's store for Store (matches its original storeId argument,
        // which filtered reviews to the staff member's store) and null - normalized to "" here, matching
        // GetAllProductReviews's expected "no filter" value - for both Admin and Vendor (matches their
        // originals, which both passed "" literally; Vendor scopes by VendorId via the HasAccess check
        // above, not by store, since VendorProductDataScope.DefaultStoreId is null).
        var storeId = scope.DefaultStoreId ?? "";

        var productReviews = await productReviewService.GetAllProductReviews("", null,
            null, null, "", storeId, productId);

        var items = new List<ProductReviewModel>();
        foreach (var item in productReviews.PagedForCommand(command))
        {
            var m = new ProductReviewModel();
            await productViewModelService.PrepareProductReviewModel(m, item, false, true);
            items.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = items,
            Total = productReviews.Count
        };

        return Json(gridModel);
    }

    #endregion

    #region Export / Import

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(ProductListModel model,
        [FromServices] IExportManager<Product> exportManager)
    {
        // No explicit scope filter needed here: productViewModelService is host-specific (Admin's
        // implementation returns all products matching the search model; Vendor's PrepareProducts always
        // constrains the SearchProducts call to WorkContext.CurrentVendor.Id regardless of what's in
        // model), so scoping is already enforced inside the polymorphic call, same as ProductList above.
        var products = await productViewModelService.PrepareProducts(model);
        try
        {
            var bytes = await exportManager.Export(products);
            return File(bytes, "text/xls", "products.xlsx");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelSelected(string selectedIds,
        [FromServices] IExportManager<Product> exportManager)
    {
        var products = new List<Product>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToArray();
            products.AddRange(await productService.GetProductsByIds(ids, true));
        }

        // Unlike ExportExcelAll, selectedIds is caller-supplied and not derived from a scoped search -
        // Vendor's original explicitly re-checked HasAccessToProduct per id for exactly this reason
        // (a vendor could otherwise pass another vendor's product id and export it). Admin's original had
        // no check at all (GlobalAdminDataScope.HasAccess is a no-op there), so applying the filter
        // unconditionally closes that gap the same way as every other row in this task, without changing
        // Admin's or Store's observable behavior.
        var scoped = new List<Product>();
        foreach (var product in products)
            if (await scope.HasAccess(product))
                scoped.Add(product);

        var bytes = await exportManager.Export(scoped);
        return File(bytes, "text/xls", "products.xlsx");
    }

    // Not virtual: Vendor's original ProductController has no ImportExcel action at all, and Vendor is
    // never granted the "Products" permission's Import action (grep across src/Web/Grand.Web.Vendor found
    // no PermissionActionName.Import usage anywhere) - vendors are deliberately not allowed to bulk-import
    // products. [PermissionAuthorizeAction(PermissionActionName.Import)] below already 403s for any host
    // whose role has no Import grant for Products, so this is safe to expose unconditionally on the base
    // class; Vendor's (future) subclass simply never routes a view to it, same as any other
    // permission-gated action already in this file.
    //
    // Concern (flagged, not fixed - out of scope for this row): ImportExcel only checks
    // `importexcelfile.Length > 0` before handing the raw stream to IImportManager<ProductDto>.Import.
    // There is no file-extension allowlist and no upper bound on Length before the stream is read into
    // memory by the importer. Commit a153496a6 hardened exactly this shape of gap (memory DoS + extension
    // bypass) for attribute file uploads; this action has the same shape and was not touched by that fix.
    // This is pre-existing behavior being ported verbatim, not something introduced by this migration -
    // worth a follow-up ticket, not a silent fix here.
    [PermissionAuthorizeAction(PermissionActionName.Import)]
    [HttpPost]
    public async Task<IActionResult> ImportExcel(IFormFile importexcelfile,
        [FromServices] IImportManager<ProductDto> importManager)
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

            Success(translationService.GetResource("Admin.Catalog.Products.Imported"));
            return RedirectToAction("List");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    #endregion

    #region Bulk editing

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> BulkEdit()
    {
        // scope.DefaultStoreId already encodes the per-host default: null for Admin/Vendor (Admin's
        // original called PrepareBulkEditListModel() with no storeId; Vendor's own separate service
        // (Grand.Web.Vendor.Interfaces.IProductViewModelService.PrepareBulkEditListModel) takes no
        // storeId parameter at all - not store-scoped), StaffStoreId for Store.
        var model = await productViewModelService.PrepareBulkEditListModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BulkEditSelect(DataSourceRequest command, BulkEditListModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;

        // BLOCKING PREREQUISITE for Task 11 (see plan's Task 10 PrepareBulkEditProductModel row and
        // Task 11's blocking-prerequisite note, added in commit a8def835a after this gap was flagged and
        // verified during review): Vendor's original bulk-edit grid was vendor-scoped -
        // PrepareBulkEditProductModel passed vendorId: CurrentVendor.Id into productService.SearchProducts,
        // so the grid only ever listed the vendor's own products. AdminShared's version isn't -
        // IProductViewModelService.PrepareBulkEditProductModel used here has no vendorId parameter (unlike
        // PrepareProductModel(AddProductModel), which supports SearchVendorId - see
        // AssociatedProductVendorId above) - and BulkEditListModel carries no SearchVendorId field either.
        // Closing this requires a ProductViewModelService/BulkEditListModel change, which this task's
        // per-row scope (BaseProductController.cs + tests only) does not permit. Not a security gap by
        // itself (a wider listing, not a mutation) - the mutate endpoints below (BulkEditUpdate/
        // BulkEditDelete) are scope-checked per item regardless and never leak another party's product -
        // but Task 11 must NOT subclass Vendor onto this base controller until Task 10 adds vendor-scoped
        // filtering here, or a vendor would see every vendor's products in this grid.
        var (bulkEditProductModels, totalCount) =
            await productViewModelService.PrepareBulkEditProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = bulkEditProductModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BulkEditUpdate(IEnumerable<BulkEditProductModel> products)
    {
        var validProducts = await FilterBulkEditProductsByAccess(products);
        if (validProducts.Count > 0) await productViewModelService.UpdateBulkEdit(validProducts);

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> BulkEditDelete(IEnumerable<BulkEditProductModel> products)
    {
        var validProducts = await FilterBulkEditProductsByAccess(products);
        if (validProducts.Count > 0) await productViewModelService.DeleteBulkEdit(validProducts);

        return new JsonResult("");
    }

    /// <summary>
    /// Filters a caller-supplied bulk-edit product list (BulkEditUpdate/BulkEditDelete) down to the ones
    /// the current user may mutate. Same shape as the DeleteSelected gap above: this is a caller-supplied
    /// list of ids being mutated in one request, and each id must be scope-checked individually before
    /// mutation, not just implicitly trusted because it appeared in a grid the user was shown.
    ///
    /// Admin's original had NO check at all here - both BulkEditUpdate and BulkEditDelete accepted a
    /// client-supplied list of ids and mutated/deleted every one of them unconditionally, a real unscoped
    /// bulk-mutate/bulk-delete IDOR (any authenticated admin user hitting these actions directly - not
    /// through the grid - could update or delete any product in the system by id).
    ///
    /// Store's original had this exact check (FilterValidProductsForStore, via CanAccessProduct /
    /// AccessToEntityByStore) - HasAccess (strict), matching CanAccessProduct's strict rule, not CanView.
    ///
    /// Vendor's original enforced the equivalent strict check (HasAccessToProduct / VendorId equality)
    /// inside its own separate service (Grand.Web.Vendor's ProductViewModelService.UpdateBulkEdit/
    /// DeleteBulkEdit), not in the controller - functionally the same per-item gate, just placed one layer
    /// down. Routing it through scope.HasAccess here reproduces that gate at the controller layer, where
    /// the shared IProductViewModelService.UpdateBulkEdit/DeleteBulkEdit used by this class does not
    /// filter internally (verified: both loop and mutate/delete every product they're given, no ownership
    /// check).
    ///
    /// No-op for Admin (GlobalAdminDataScope.HasAccess is always true, so validProducts == products).
    /// Silently drops missing/inaccessible ids rather than throwing (matches Admin's/Vendor's
    /// null-then-skip behavior, not Store's original throw-on-missing-id - the majority behavior, and a
    /// single bad id in a bulk request shouldn't fail the whole batch).
    /// </summary>
    private async Task<List<BulkEditProductModel>> FilterBulkEditProductsByAccess(
        IEnumerable<BulkEditProductModel> products)
    {
        if (products == null) return [];

        var byId = products
            .Where(x => !string.IsNullOrEmpty(x.Id))
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First());
        if (byId.Count == 0) return [];

        var loadedProducts = await productService.GetProductsByIds(byId.Keys.ToArray(), true);
        var validProducts = new List<BulkEditProductModel>();
        foreach (var product in loadedProducts)
            if (await scope.HasAccess(product))
                validProducts.Add(byId[product.Id]);

        return validProducts;
    }

    #endregion

    #region Product currency price

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict), not CanView: mirrors Store's CanAccessProduct (AccessToEntityByStore) check
        // on this action. Applying it uniformly also closes real gaps on the mutate actions below: Store's
        // original checked access only on List/Insert (ProductPriceUpdate/ProductPriceDelete had no check
        // at all), and Vendor's original checked access only on List (ProductPriceInsert/Update/Delete had
        // no check at all) - both let another party's product prices be updated/deleted by id.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var items = new List<ProductModel.ProductPriceModel>();
        foreach (var item in product.ProductPrices)
            items.Add(new ProductModel.ProductPriceModel {
                Id = item.Id,
                CurrencyCode = item.CurrencyCode,
                Price = item.Price,
                ProductId = product.Id
            });

        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceInsert(ProductModel.ProductPriceModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (product.ProductPrices.Any(x => x.CurrencyCode == model.CurrencyCode))
            throw new ArgumentException("Currency code exists");

        if (ModelState.IsValid)
            try
            {
                await productService.InsertProductPrice(new ProductPrice {
                    ProductId = product.Id,
                    CurrencyCode = model.CurrencyCode,
                    Price = model.Price
                });
                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceUpdate(ProductModel.ProductPriceModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productPrice = product.ProductPrices.FirstOrDefault(x => x.Id == model.Id);
        if (productPrice == null)
            throw new ArgumentException("Product price model not exists");

        if (product.ProductPrices.Any(x => x.Id != model.Id && x.CurrencyCode == model.CurrencyCode))
            throw new ArgumentException("You can't use this currency code");

        if (ModelState.IsValid)
            try
            {
                productPrice!.CurrencyCode = model.CurrencyCode;
                productPrice.Price = model.Price;
                productPrice.ProductId = model.ProductId;

                await productService.UpdateProductPrice(productPrice);

                return new JsonResult("");
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceDelete(ProductModel.ProductPriceModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productPrice = product.ProductPrices.FirstOrDefault(x => x.Id == model.Id);
        if (productPrice == null)
            throw new ArgumentException("Product price model not exists");

        if (ModelState.IsValid)
        {
            productPrice!.ProductId = model.ProductId;
            await productService.DeleteProductPrice(productPrice);

            return new JsonResult("");
        }

        // ErrorForKendoGridJson(ModelState), not Content(ModelState.GetErrors()): matches Admin/Store.
        // Vendor's original used the Vendor-only GetErrors() extension here (and inconsistently, since
        // several of Vendor's *other* grid actions in this same file already use ErrorForKendoGridJson) -
        // not a deliberate host-specific contract, just Vendor's own inconsistency. AdminShared cannot
        // reference Grand.Web.Vendor.Extensions.ModelStateExtensions.GetErrors() from this project anyway.
        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Tier prices

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> TierPriceList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess on List (Store/Vendor both checked here; Admin's Global scope is a no-op). Closes the
        // same class of gap as "Product currency price": Vendor's original checked ownership only on List
        // and TierPriceEditPopup(GET) - TierPriceCreatePopup(POST), TierPriceEditPopup(POST) and
        // TierPriceDelete had NO ownership check at all, so a vendor could create/update/delete a tier
        // price on any product (not just their own) by posting a known productId. Applying scope.HasAccess
        // uniformly on every mutating action below closes that.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        // Old storeId-parameter overload (still present pending Task 9/10); scope.DefaultStoreId is null for
        // Admin/Vendor (Global/VendorProduct scopes) and the staff store for Store, same as the other rows
        // still on this signature.
        var tierPricesModel = await productViewModelService.PrepareTierPriceModel(product, scope.DefaultStoreId ?? "");
        var gridModel = new DataSourceResult {
            Data = tierPricesModel,
            Total = tierPricesModel.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> TierPriceCreatePopup(string productId)
    {
        var model = new ProductModel.TierPriceModel {
            ProductId = productId
        };
        await productViewModelService.PrepareTierPriceModel(model, scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> TierPriceCreatePopup(ProductModel.TierPriceModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            // Vendor's original never even loaded the product here - it inserted straight off
            // model.ProductId with no ownership check at all. See the List-action comment above.
            if (!await scope.HasAccess(product))
                return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            var tierPrice = model.ToEntity(dateTimeService);
            await productService.InsertTierPrice(tierPrice, product.Id);

            return Content("");
        }

        Error(ModelState);
        //If we got this far, something failed, redisplay form
        await productViewModelService.PrepareTierPriceModel(model, scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> TierPriceEditPopup(string id, string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess here too: Store's original never checked ownership on this GET action (only on the
        // List/Create-POST/Edit-POST/Delete siblings), which would let store staff open (read-only,
        // via this popup) the tier-price edit view for a product outside their store. Vendor's original did
        // check (HasAccessToProduct), so this closes Store's gap while keeping Vendor's existing behavior.
        if (!await scope.HasAccess(product))
            return Content("This is not your product");

        var tierPrice = product.TierPrices.FirstOrDefault(x => x.Id == id);
        if (tierPrice == null)
            return Content("Empty tier price");

        var model = tierPrice.ToModel(dateTimeService);
        model.ProductId = productId;
        await productViewModelService.PrepareTierPriceModel(model, scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> TierPriceEditPopup(string productId, ProductModel.TierPriceModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(productId, true);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            // See the List-action comment: Vendor's original had no ownership check on this POST at all.
            if (!await scope.HasAccess(product))
                return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            var tierPrice = product.TierPrices.FirstOrDefault(x => x.Id == model.Id);
            if (tierPrice == null)
                return Content("Empty tier price");

            tierPrice = model.ToEntity(tierPrice, dateTimeService);
            await productService.UpdateTierPrice(tierPrice, product.Id);

            return Content("");
        }

        Error(ModelState);
        //stores
        await productViewModelService.PrepareTierPriceModel(model, scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> TierPriceDelete(ProductModel.TierPriceDeleteModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId, true);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            // See the List-action comment: Vendor's original had no ownership check on Delete at all.
            if (!await scope.HasAccess(product))
                return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            var tierPrice = product.TierPrices.FirstOrDefault(x => x.Id == model.Id);
            if (tierPrice == null)
                throw new ArgumentException("No tier price found with the specified id");

            await productService.DeleteTierPrice(tierPrice, product.Id);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Product attributes

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeMappingList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess applied uniformly across this region (Admin's Global scope is a no-op). Store originally
        // checked ownership here (CanAccessProduct) and Vendor did too (CheckAccessToProduct), but neither
        // host checked ProductAttributeMappingPopup(POST) or ProductAttributeValidationRulesPopup(POST) at
        // all - a store/vendor user could edit an attribute mapping's name/values or its validation rules on
        // any product (not just their own/in-scope one) by posting a known productId. Vendor's
        // ProductAttributeMappingPopup(POST) had no check either, despite its own GET sibling checking.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var attributesModel = await productViewModelService.PrepareProductAttributeMappingModels(product);
        var gridModel = new DataSourceResult {
            Data = attributesModel,
            Total = attributesModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeMappingPopup(string productId, string productAttributeMappingId)
    {
        var product = await productService.GetProductById(productId);

        // See the List-action comment above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (string.IsNullOrEmpty(productAttributeMappingId))
        {
            var model = await productViewModelService.PrepareProductAttributeMappingModel(product);
            return View(model);
        }

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        var editModel = await productViewModelService.PrepareProductAttributeMappingModel(product,
            productAttributeMapping);
        return View(editModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeMappingPopup(ProductModel.ProductAttributeMappingModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            // HasAccess here too: Vendor's original never checked ownership on this POST at all (only its
            // GET sibling did), letting a vendor insert/update an attribute mapping on any product. See the
            // List-action comment.
            if (!await scope.HasAccess(product))
                return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            if (string.IsNullOrEmpty(model.Id))
                await productViewModelService.InsertProductAttributeMappingModel(model);
            else
                await productViewModelService.UpdateProductAttributeMappingModel(model);

            return Content("");
        }

        Error(ModelState);
        model = await productViewModelService.PrepareProductAttributeMappingModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeMappingDelete(string id, string productId,
        [FromServices] IProductAttributeService productAttributeService)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productAttributeMapping = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == id);
        if (productAttributeMapping == null)
            throw new ArgumentException("No product attribute mapping found with the specified id");

        // See the List-action comment above.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        await productAttributeService.DeleteProductAttributeMapping(productAttributeMapping, product.Id);
        return new JsonResult("");
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeValidationRulesPopup(string id, string productId)
    {
        var product = await productService.GetProductById(productId);

        // See the List-action comment above. Store's original used ErrorForKendoGridJson here even though
        // this action returns a View (not a grid), which would have rendered raw JSON as page content on
        // denial - using Content(...) instead, consistent with the other popup GET action in this region.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productAttributeMapping = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == id);
        if (productAttributeMapping == null)
            return Content("No attribute value found with the specified id");

        var model = await productViewModelService.PrepareProductAttributeMappingModel(productAttributeMapping);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeValidationRulesPopup(
        ProductModel.ProductAttributeMappingModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productAttributeMapping = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.Id);
        if (productAttributeMapping == null)
            throw new ArgumentException("No attribute value found with the specified id");

        // HasAccess here too: none of the three original hosts checked ownership on this POST at all -
        // a store/vendor user could update an attribute mapping's validation rules (min/max length,
        // allowed file extensions, default value) on any product by posting a known productId/model.Id.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.UpdateProductAttributeValidationRulesModel(productAttributeMapping, model);
            return Content("");
        }

        Error(ModelState);
        model = await productViewModelService.PrepareProductAttributeMappingModel(productAttributeMapping);
        return View(model);
    }

    #endregion

    #region Product attributes. Condition

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeConditionPopup(string productId, string productAttributeMappingId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess applied uniformly across this region (Admin's Global scope is a no-op). Admin's original
        // had no ownership check on either action of this region. See the "Product attributes" region above
        // for the same pattern.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (productAttributeMapping == null)
            //No attribute value found with the specified id
            return Content("No attribute value found with the specified id");

        var model = await productViewModelService.PrepareProductAttributeConditionModel(product,
            productAttributeMapping);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeConditionPopup(ProductAttributeConditionModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.ProductAttributeMappingId);
        if (productAttributeMapping == null)
            return Content("No attribute value found with the specified id");

        // ModelState.IsValid: Vendor's original wrapped this action in a ModelState.IsValid check and
        // returned ModelState.GetErrors() on failure; Admin's and Store's originals had no such check.
        // That check was not inert: Vendor's ProductAttributeConditionModel implements the
        // IProductValidVendor marker interface, which the global ValidationFilter resolves to
        // ProductValidVendor (a FluentValidation rule checking product.VendorId == CurrentVendor.Id) -
        // real ownership enforcement, not a no-op. Dropping it here is safe because the scope.HasAccess
        // (product) call below now performs the equivalent check directly against the loaded entity,
        // which HasAccess.cs's own doc comment argues is more robust than re-deriving ownership from a
        // request field.
        //
        // HasAccess here too: Vendor's original never checked ownership on this POST at all (only its GET
        // sibling did, via CheckAccessToProduct), letting a vendor update an attribute condition on any
        // product by posting a known productId/productAttributeMappingId. Admin's original had no check on
        // either action.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        await productViewModelService.UpdateProductAttributeConditionModel(product, productAttributeMapping, model);
        return Content("");
    }

    #endregion

    #region Product attribute values

    //list
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> EditAttributeValues(string productAttributeMappingId, string productId,
        [FromServices] IProductAttributeService productAttributeService)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess applied uniformly across this region (Admin's Global scope is a no-op). Admin's
        // original had no ownership check on this action at all.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (productAttributeMapping == null)
            throw new ArgumentException("No product attribute mapping found with the specified id");

        var productAttribute =
            await productAttributeService.GetProductAttributeById(productAttributeMapping.ProductAttributeId);
        var model = new ProductModel.ProductAttributeValueListModel {
            ProductName = product.Name,
            ProductId = product.Id,
            ProductAttributeName = productAttribute.Name,
            ProductAttributeMappingId = productAttributeMappingId
        };

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeValueList(string productAttributeMappingId, string productId,
        DataSourceRequest command)
    {
        var product = await productService.GetProductById(productId);

        // See the EditAttributeValues comment above.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(
                translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (productAttributeMapping == null)
            throw new ArgumentException("No product attribute mapping found with the specified id");

        var values =
            await productViewModelService.PrepareProductAttributeValueModels(product, productAttributeMapping);
        var gridModel = new DataSourceResult {
            Data = values,
            Total = values.Count
        };
        return Json(gridModel);
    }

    //create
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeValueCreatePopup(string productAttributeMappingId,
        string productId)
    {
        var product = await productService.GetProductById(productId);

        // Content(...), not ErrorForKendoGridJson: Store's original used ErrorForKendoGridJson here even
        // though this action returns a View (not a grid), which would have rendered raw JSON as page
        // content on denial - using Content(...) instead, consistent with the sibling GET popup actions
        // in this region and elsewhere in this file.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (productAttributeMapping == null)
            throw new ArgumentException("No product attribute mapping found with the specified id");

        var model =
            await productViewModelService.PrepareProductAttributeValueModel(product, productAttributeMapping);
        //locales
        await AddLocales(languageService, model.Locales);

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeValueCreatePopup(ProductModel.ProductAttributeValueModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess added explicitly rather than relying on validation-layer side effects. Admin has no
        // ownership concept at all (GlobalAdminDataScope.HasAccess is always true), so Admin was never at
        // risk here despite having neither an explicit check nor a validator. Store's shared
        // ProductAttributeValueModelValidator (BaseStoreAccessValidator<...>) enforces ownership whenever
        // StaffStoreId is set, so Store's original had validator-layer coverage. The actual risk this
        // guards against is to VENDOR: Vendor's original model implements IProductValidVendor, so the
        // global ValidationFilter resolves IValidator<IProductValidVendor> (ProductValidVendor) and adds a
        // ModelState error when product.VendorId doesn't match the current vendor - a real check, not a
        // no-op - but ValidationFilter never short-circuits a non-JSON POST (see
        // ValidationFilter.OnActionExecutionAsync), so that protection only actually held because Vendor's
        // original action gated the insert behind `if (ModelState.IsValid)`. Once this action moves to the
        // shared AdminShared model (which does not implement IProductValidVendor), Vendor would silently
        // lose that guard in the merge unless replaced - scope.HasAccess is that replacement, and now
        // applies uniformly (a no-op for Admin, equivalent-or-stronger for Store/Vendor) instead of leaning
        // on a marker-interface side effect that only one host had.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.ProductAttributeMappingId);
        if (productAttributeMapping == null)
            //No product attribute found with the specified id
            return RedirectToAction("List", "Product");

        if (ModelState.IsValid)
        {
            await productViewModelService.InsertProductAttributeValueModel(model);
            return Content("");
        }

        //If we got this far, something failed, redisplay form
        await productViewModelService.PrepareProductAttributeValueModel(product, model);
        return View(model);
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAttributeValueEditPopup(string id, string productId,
        string productAttributeMappingId)
    {
        var product = await productService.GetProductById(productId);

        // See the EditAttributeValues comment above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var pa = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        if (pa == null)
            return RedirectToAction("List", "Product");

        var pav = pa.ProductAttributeValues.FirstOrDefault(x => x.Id == id);
        if (pav == null)
            //No attribute value found with the specified id
            return RedirectToAction("List", "Product");

        var model = await productViewModelService.PrepareProductAttributeValueModel(pa, pav);
        //locales
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = pav.GetTranslation(x => x.Name, languageId, false);
        });
        //pictures
        await productViewModelService.PrepareProductAttributeValueModel(product, model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeValueEditPopup(string productId,
        ProductModel.ProductAttributeValueModel model)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // See the ProductAttributeValueCreatePopup(POST) comment above re: the validator's coverage gap -
        // the same reasoning applies here (this action shares the same model type). Unlike CreatePopup(POST),
        // Vendor's original for THIS action already had an explicit inline check
        // (`if (product == null || !_contextAccessor.WorkContext.HasAccessToProduct(product)) throw ...`),
        // so scope.HasAccess is a mechanical substitution there, not a fix. Admin's original had no check
        // on either action of this pair.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var pav = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == model.ProductAttributeMappingId)
            ?.ProductAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (pav == null)
            //No attribute value found with the specified id
            return RedirectToAction("List", "Product");

        if (ModelState.IsValid)
        {
            await productViewModelService.UpdateProductAttributeValueModel(pav, model);
            return Content("");
        }

        //If we got this far, something failed, redisplay form
        await productViewModelService.PrepareProductAttributeValueModel(product, model);
        return View(model);
    }

    //delete
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeValueDelete(string id, string pam, string productId,
        [FromServices] IProductAttributeService productAttributeService)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess added here: this action takes only simple string parameters (no complex-typed POST
        // body), so none of the model-level validators discussed above ever run for it. Admin's original
        // had no ownership check at all; Vendor's/Store's explicit inline checks are what scope.HasAccess
        // now replaces uniformly.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(
                translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var pav = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == pam)?.ProductAttributeValues
            .FirstOrDefault(x => x.Id == id);
        if (pav == null)
            throw new ArgumentException("No product attribute value found with the specified id");

        if (ModelState.IsValid)
        {
            await productAttributeService.DeleteProductAttributeValue(pav, productId, pam);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    public async Task<IActionResult> AssociateProductToAttributeValuePopup()
    {
        // scope.DefaultStoreId ?? "": matches Store's original (passed StaffStoreId to scope the search to
        // the staff member's store); null for Admin/Vendor (no store concept), matching their originals
        // (no argument, defaulting to "").
        var model =
            await productViewModelService.PrepareAssociateProductToAttributeValueModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociateProductToAttributeValuePopupList(DataSourceRequest command,
        ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel model)
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociateProductToAttributeValuePopup(
        ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel model)
    {
        var associatedProduct = await productService.GetProductById(model.AssociatedToProductId);
        if (associatedProduct == null)
            return Content("Cannot load a product");

        // HasAccess on the associated product: Admin's original had no check here. Vendor's/Store's
        // originals checked ownership of the *associated* product (the one about to be referenced as an
        // AssociatedToProductId value) - there is no separate "owning" product in scope for this action, so
        // the associated product is the only entity available to check.
        if (!await scope.HasAccess(associatedProduct))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        return Content("");
    }

    #endregion

    #region Product attribute combinations

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess added here: Admin's original had no ownership check on this grid at all. Store's
        // original used CanAccessProduct + a hardcoded "Admin.Catalog.Products.Permissions" resource key
        // (even though it's the Store host); Vendor's original used a local CheckAccessToProduct helper
        // that returned a plain hardcoded string ("This is not your product" / "Product not exists")
        // rather than a resource key. scope.HasAccess plus the scope.ResourceKeyPrefix-qualified resource
        // key normalizes all three to the convention used throughout this file.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(
                translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var combinationsModel = await productViewModelService.PrepareProductAttributeCombinationModel(product);
        var gridModel = new DataSourceResult {
            Data = combinationsModel,
            Total = combinationsModel.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationDelete(string id, string productId,
        [FromServices] IProductAttributeService productAttributeService)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess added here, checked before the combination lookup: matches the
        // ProductAttributeValueDelete precedent above (deny before revealing whether the sub-entity
        // exists). Vendor's original combined the null-check and the ownership check into a single throw
        // (an unhandled exception on denial); normalized here to the file's ErrorForKendoGridJson
        // convention - this is a Kendo grid row-delete action - matching how Store's original reported
        // denial (though Store checked access after the combination lookup, not before).
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(
                translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var combination = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == id);
        if (combination == null)
            throw new ArgumentException("No product attribute combination found with the specified id");

        await productAttributeService.DeleteProductAttributeCombination(combination, productId);
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
        {
            var pr = await productService.GetProductById(productId);
            pr.StockQuantity = pr.ProductAttributeCombinations.Sum(x => x.StockQuantity);
            pr.ReservedQuantity = pr.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
            await inventoryManageService.UpdateStockProduct(pr, false);
        }

        return new JsonResult("");
    }

    //edit
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AttributeCombinationPopup(string productId, string id)
    {
        var product = await productService.GetProductById(productId);

        // Content(...), not ErrorForKendoGridJson: Store's original used ErrorForKendoGridJson here even
        // though this action returns a View inside a magnificPopup modal (not a grid), which would render
        // raw JSON as page content on denial - using Content(...) instead, consistent with the
        // EditAttributeValues GET popup correction in region "Product attribute values" above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var model = await productViewModelService.PrepareProductAttributeCombinationModel(product, id);
        await productViewModelService.PrepareAddProductAttributeCombinationModel(model, product);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AttributeCombinationPopup(string productId,
        ProductAttributeCombinationModel model)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            //No product found with the specified id
            return RedirectToAction("List", "Product");

        // Content(...) on denial, matching Store's original. Vendor's original folded denial into the
        // same redirect used for a missing product; Content(...) surfaces the permissions message instead
        // of silently redirecting, matching every other Edit-scoped action in this region.
        // ProductAttributeCombinationModel (Grand.Web.AdminShared.Models.Catalog) does not implement
        // IProductValidVendor and has no registered FluentValidation validator, so there is no
        // validator-layer ownership check being displaced here - scope.HasAccess is the only guard.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var warnings = await productViewModelService.InsertOrUpdateProductAttributeCombinationPopup(product, model);
        if (!warnings.Any()) return Content("");
        //If we got this far, something failed, redisplay form
        await productViewModelService.PrepareAddProductAttributeCombinationModel(model, product);
        model.Warnings = warnings;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> GenerateAllAttributeCombinations(string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // ErrorForKendoGridJson(...), not Content(...): this action is invoked via a dataType:'json' ajax
        // call (see CreateOrUpdate.ProductAttributes.TabAttributeCombinations.cshtml). Store's original
        // returned Content(resource) on denial - a plain string the client's dataType:'json' parser
        // cannot parse - which threw a JSON-parse exception and fell into the ajax error callback,
        // showing a generic "Error while generating attribute combinations" alert. Vendor's original
        // folded denial into the null-product throw (also an unhandled exception). Normalized here to a
        // real JSON response, which avoids that parse-exception/generic-alert path - but note the
        // .cshtml's ajax success callback for this action ignores the response body entirely (it just
        // unconditionally refreshes the grid), so the denial still isn't surfaced to the user; it's a
        // silent no-op instead of a visible (if wrong) error. Server-side enforcement is correct either
        // way - nothing is generated on denial - this is a pre-existing view-layer gap, out of scope for
        // this controller-only row (flagged for Phase 2 view work).
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(
                translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        await productViewModelService.GenerateAllAttributeCombinations(product);

        return Json(new { Success = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ClearAllAttributeCombinations(string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // See the GenerateAllAttributeCombinations comment above - same dataType:'json' contract, same fix.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(
                translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (ModelState.IsValid)
        {
            await productViewModelService.ClearAllAttributeCombinations(product);

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            {
                product.StockQuantity = 0;
                product.ReservedQuantity = 0;
                await inventoryManageService.UpdateStockProduct(product, false);
            }

            return Json(new { Success = true });
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion

    #region Product Attribute combination - tier prices

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationTierPriceList(DataSourceRequest command,
        string productId, string productAttributeCombinationId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess added here: Admin's original had no ownership check at all on this region (any of its
        // four actions). Store checked CanAccessProduct on every action. Vendor's List/Delete used explicit
        // checks (CheckAccessToProduct / HasAccessToProduct); Vendor's Insert/Update relied entirely on
        // ProductAttributeCombinationTierPricesModel implementing IProductValidVendor (the global
        // ValidationFilter resolves ProductValidVendor, a FluentValidation rule comparing
        // product.VendorId to CurrentVendor.Id, and the action only proceeded inside `if
        // (ModelState.IsValid)`) - a real check, not a no-op, but the shared AdminShared model used here
        // does not implement that marker interface, so merging Vendor's Insert/Update as-is would silently
        // drop vendor ownership enforcement. scope.HasAccess replaces it uniformly across all four actions
        // (no-op for Admin, equivalent-or-stronger for Store/Vendor).
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(
                translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var tierPriceModel =
            await productViewModelService.PrepareProductAttributeCombinationTierPricesModel(product,
                productAttributeCombinationId);
        var gridModel = new DataSourceResult {
            Data = tierPriceModel,
            Total = tierPriceModel.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationTierPriceInsert(string productId,
        string productAttributeCombinationId, ProductModel.ProductAttributeCombinationTierPricesModel model)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // See the List comment above - this is the action where Vendor's IProductValidVendor-driven check
        // would have silently disappeared without an explicit scope.HasAccess call.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var combination =
            product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == productAttributeCombinationId);
        if (combination != null)
            await productViewModelService.InsertProductAttributeCombinationTierPricesModel(product, combination,
                model);

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationTierPriceUpdate(string productId,
        string productAttributeCombinationId, ProductModel.ProductAttributeCombinationTierPricesModel model)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // See the List comment above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var combination = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == productAttributeCombinationId);
        if (combination != null)
            await productViewModelService.UpdateProductAttributeCombinationTierPricesModel(product, combination,
                model);

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeCombinationTierPriceDelete(string productId,
        string productAttributeCombinationId, string id)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess here matches Store's CanAccessProduct and Vendor's HasAccessToProduct checks (both
        // already present on Delete in the originals); normalized to scope.HasAccess for Admin too.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var combination = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == productAttributeCombinationId);
        if (combination != null)
        {
            var tierPrice = combination.TierPrices.FirstOrDefault(x => x.Id == id);
            if (tierPrice != null)
                await productViewModelService.DeleteProductAttributeCombinationTierPrices(product, combination,
                    tierPrice);
        }

        return new JsonResult("");
    }

    #endregion

    #region Reservation

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ListReservations(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct checks;
        // Admin's original had no check at all - normalized to scope.HasAccess for all three hosts.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var reservations =
            await productReservationService.GetProductReservationsByProductId(productId, null, null,
                command.Page - 1, command.PageSize);
        var reservationModel = reservations
            .Select(x => new ProductModel.ReservationModel {
                ReservationId = x.Id,
                Date = x.Date,
                OrderId = x.OrderId,
                ProductId = x.ProductId,
                Parameter = x.Parameter,
                Resource = x.Resource,
                Duration = x.Duration
            }).ToList();

        var gridModel = new DataSourceResult {
            Data = reservationModel,
            Total = reservations.TotalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> GenerateCalendar(string productId, ProductModel.GenerateCalendarModel model)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess (strict), returning the same {errors:...} JSON shape as Store's CanAccessProduct
        // check (the client's ajax success handler reads data.errors on denial). Vendor's original threw
        // ArgumentException for the combined null-or-access-denied case instead, which the client's
        // Kendo/ajax error callback only surfaces as a generic "Error" alert - returning the JSON errors
        // message here is strictly more informative and closes Admin's original gap (no check at all).
        if (!await scope.HasAccess(product))
            return Json(new { errors = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions") });

        var reservations = await productReservationService.GetProductReservationsByProductId(productId, null, null);
        if (reservations.Any())
            if (((product.IntervalUnitId == IntervalUnit.Minute || product.IntervalUnitId == IntervalUnit.Hour) &&
                 (IntervalUnit)model.Interval == IntervalUnit.Day) ||
                (product.IntervalUnitId == IntervalUnit.Day &&
                 ((IntervalUnit)model.IntervalUnit == IntervalUnit.Minute ||
                  (IntervalUnit)model.IntervalUnit == IntervalUnit.Hour)))
                return Json(new {
                    errors = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Calendar.CannotChangeInterval")
                });

        if (!ModelState.IsValid)
        {
            var error = (Dictionary<string, Dictionary<string, object>>)ModelState.SerializeErrors();
            var s = "";
            foreach (var error1 in error)
                foreach (var error2 in error1.Value)
                {
                    var v = (string[])error2.Value;
                    s += v[0] + "\n";
                }

            return Json(new { errors = s });
        }

        //update fields on product
        await productService.UpdateProductField(product, x => x.Interval, model.Interval);
        await productService.UpdateProductField(product, x => x.IntervalUnitId, (IntervalUnit)model.IntervalUnit);
        await productService.UpdateProductField(product, x => x.IncBothDate, model.IncBothDate);

        var minutesToAdd = (IntervalUnit)model.IntervalUnit switch {
            IntervalUnit.Minute => model.Interval,
            IntervalUnit.Hour => model.Interval * 60,
            IntervalUnit.Day => model.Interval * 60 * 24,
            _ => 0
        };

        var _hourFrom = model.StartTime.Hour;
        var _minutesFrom = model.StartTime.Minute;
        var _hourTo = model.EndTime.Hour;
        var _minutesTo = model.EndTime.Minute;
        var _dateFrom = new DateTime(model.StartDate.Value.Year, model.StartDate.Value.Month, model.StartDate.Value.Day,
            0, 0, 0, 0);
        var _dateTo = new DateTime(model.EndDate.Value.Year, model.EndDate.Value.Month, model.EndDate.Value.Day, 23, 59,
            59, 999);
        if ((IntervalUnit)model.IntervalUnit == IntervalUnit.Day)
        {
            model.Quantity = 1;
            model.Parameter = "";
        }
        else
        {
            model.Resource = "";
        }

        var dates = new List<DateTime>();
        var counter = 0;
        for (var iterator = _dateFrom; iterator <= _dateTo; iterator += new TimeSpan(0, minutesToAdd, 0))
        {
            if ((IntervalUnit)model.IntervalUnit != IntervalUnit.Day)
            {
                if (iterator.Hour >= _hourFrom && iterator.Hour <= _hourTo)
                {
                    if (iterator.Hour == _hourTo)
                        if (iterator.Minute > _minutesTo)
                            continue;
                    if (iterator.Hour == _hourFrom)
                        if (iterator.Minute < _minutesFrom)
                            continue;
                }
                else
                {
                    continue;
                }
            }

            if ((iterator.DayOfWeek == DayOfWeek.Monday && !model.Monday) ||
                (iterator.DayOfWeek == DayOfWeek.Tuesday && !model.Tuesday) ||
                (iterator.DayOfWeek == DayOfWeek.Wednesday && !model.Wednesday) ||
                (iterator.DayOfWeek == DayOfWeek.Thursday && !model.Thursday) ||
                (iterator.DayOfWeek == DayOfWeek.Friday && !model.Friday) ||
                (iterator.DayOfWeek == DayOfWeek.Saturday && !model.Saturday) ||
                (iterator.DayOfWeek == DayOfWeek.Sunday && !model.Sunday))
                continue;

            for (var i = 0; i < model.Quantity.MaxQuantity(); i++)
            {
                dates.Add(iterator);
                try
                {
                    var insert = true;
                    if ((IntervalUnit)model.IntervalUnit == IntervalUnit.Day)
                        if (reservations.Any(x => x.Resource == model.Resource && x.Date == iterator))
                            insert = false;
                    if (insert)
                    {
                        if (counter++ > 1000)
                            break;

                        await productReservationService.InsertProductReservation(new ProductReservation {
                            OrderId = "",
                            Date = iterator,
                            ProductId = productId,
                            Resource = model.Resource,
                            Parameter = model.Parameter,
                            Duration = model.Interval + " " + enumTranslationService.GetTranslationEnum((IntervalUnit)model.IntervalUnit)
                        });
                    }
                }
                catch { }
            }
        }

        return Json(new { success = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ClearCalendar(string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // Throw, not Json({errors}): unlike GenerateCalendar, this view's success callback never reads
        // the response body at all (see CreateOrUpdate.Calendar.cshtml on all three hosts - identical,
        // no else branch) - a 200 here would silently refresh the grid as if the clear had succeeded. A
        // thrown exception at least surfaces a generic error via the ajax error callback. The view not
        // reading a denial message is a pre-existing gap, out of scope for this migration.
        if (!await scope.HasAccess(product))
            throw new ArgumentException(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var toDelete = await productReservationService.GetProductReservationsByProductId(productId, true, null);
        foreach (var record in toDelete) await productReservationService.DeleteProductReservation(record);

        return Json("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ClearOld(string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // Throw, not Json({errors}): see the comment on ClearCalendar above - this view's success
        // callback doesn't read the response body either.
        if (!await scope.HasAccess(product))
            throw new ArgumentException(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var toDelete =
            (await productReservationService.GetProductReservationsByProductId(productId, true, null)).Where(x =>
                x.Date < DateTime.UtcNow);
        foreach (var record in toDelete) await productReservationService.DeleteProductReservation(record);

        return Json("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductReservationDelete(ProductModel.ReservationModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var toDelete = await productReservationService.GetProductReservation(model.ReservationId);

        // Cross-product IDOR closed here: none of the three original hosts verified that the reservation
        // being deleted (looked up purely by model.ReservationId) actually belongs to the product just
        // access-checked (model.ProductId). Both are independent, attacker-supplied POST fields - Store
        // and Vendor's original code let a caller who owns/has-access-to *any* product pass the access
        // check with that product's id while supplying a ReservationId belonging to a different, unowned
        // product, deleting a reservation on a product they have no access to.
        if (toDelete != null && toDelete.ProductId != product.Id)
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (toDelete != null)
        {
            if (string.IsNullOrEmpty(toDelete.OrderId))
                await productReservationService.DeleteProductReservation(toDelete);
            else
                return Json(new DataSourceResult {
                    Errors = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.ProductReservations.CantDeleteWithOrder")
                });
        }

        return Json("");
    }

    #endregion

    #region Bids

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ListBids(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        // HasAccess: mirrors Store's CanAccessProduct and Vendor's HasAccessToProduct checks; Admin's
        // original had no check at all - normalized to scope.HasAccess for all three hosts.
        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var (bidModels, totalCount) =
            await productViewModelService.PrepareBidMode(productId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = bidModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BidDelete(ProductModel.BidModel model)
    {
        var product = await productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        if (!await scope.HasAccess(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var toDelete = await auctionService.GetBid(model.BidId);

        // Cross-product IDOR closed here, identical shape to ProductReservationDelete above: none of the
        // three original hosts verified that the bid being deleted (looked up purely by model.BidId)
        // actually belongs to the product just access-checked (model.ProductId). Both are independent,
        // attacker-supplied POST fields - Store and Vendor's original code let a caller who owns/has
        // access to *any* product pass the access check using that product's id while supplying a BidId
        // belonging to a different, unowned product, deleting a bid on a product they have no access to.
        if (toDelete != null && toDelete.ProductId != product.Id)
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        if (toDelete != null)
        {
            if (string.IsNullOrEmpty(toDelete.OrderId))
            {
                //delete bid
                await auctionService.DeleteBid(toDelete);
                return Json("");
            }

            return Json(new DataSourceResult {
                Errors = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Bids.CantDeleteWithOrder")
            });
        }

        return Json(new DataSourceResult { Errors = "Bid not exists" });
    }

    #endregion
}
