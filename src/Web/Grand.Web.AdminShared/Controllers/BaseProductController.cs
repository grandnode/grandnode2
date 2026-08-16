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
}
