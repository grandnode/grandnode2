using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
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

[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IInventoryManageService inventoryManageService,
    ILanguageService languageService,
    IRequestTranslationService translationService,
    IProductReservationService productReservationService,
    IAuctionService auctionService,
    IDateTimeService dateTimeService,
    IRequestPermissionService permissionService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    protected virtual void EditWarningCheck(Product product) { }

    protected IRequestTranslationService TranslationService => translationService;
    protected IAdminDataScope<Product> Scope => scope;

    #region Product list / create / edit / delete

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        var model = await productViewModelService.PrepareProductListModel();
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

    /// <param name="popup">
    /// true: the screen opened inside a popup of the bulk-edit grid (EditPopup) - no panel chrome,
    /// only the quick-edit tabs shown, and every save stays in the popup.
    /// </param>
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id, bool popup = false)
    {
        var product = await productService.GetProductById(id, true);
        if (product == null) return RedirectToAction("List");

        EditWarningCheck(product);
        // CanView, not HasAccess: viewing a shared/global product is allowed on Store (with a warning
        // from EditWarningCheck above); only mutating one is restricted to the exclusive single-store
        // owner. See IAdminDataScope<TEntity>.CanView's doc comment and StoreAdminDataScope.CanView.
        if (!await scope.CanView(product)) return RedirectToAction("List");
        //seen but not owned (Store: a product shared with other stores): the tabs render without the
        //buttons that write; every write action still checks HasAccess itself
        ViewBag.IsReadOnly = !await scope.HasAccess(product);

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

        return popup ? View("EditPopup", model) : View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(ProductModel model, bool continueEditing, bool popup = false)
    {
        var product = await productService.GetProductById(model.Id, true);
        if (product == null) return RedirectToAction("List");
        if (!await scope.HasAccess(product)) return RedirectToAction("Edit", new { id = product.Id, popup });

        if (model.Ticks != product.Ticks)
        {
            Error(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Fields.ChangedWarning"));
            return RedirectToAction("Edit", new { id = product.Id, popup });
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
            //a popup has no list to go back to: it stays on the tab that was saved
            if (continueEditing || popup)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = product.Id, popup });
            }

            return RedirectToAction("List");
        }

        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        await productViewModelService.PrepareProductModel(model, product, false, true);
        return popup ? View("EditPopup", model) : View(model);
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
        var products = await productService.GetProductsByIds(selectedIds.ToArray(),
            access: CatalogAccess.IncludingHidden());
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

            var products = await productService.GetProductsByIds(ids.ToArray(),
                access: CatalogAccess.IncludingHidden());
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
        // PrepareAddRequiredProductModel() now reads scope internally (Task 9) - the injected
        // IAdminDataScope<Product> already encodes the per-host default exactly: null for Admin
        // (global) and Vendor (not store-scoped), StaffStoreId for Store.
        var model = await productViewModelService.PrepareAddRequiredProductModel();
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

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Admin's original had
        // no check at all - GlobalAdminDataScope.HasAccess is a no-op there, so this closes that gap the
        // same way as every other row in this task.
        if (!await scope.CanView(product))
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

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original had no check at all - GlobalAdminDataScope.HasAccess
        // is a no-op there, so this closes that gap the same way as every other row in this task.
        if (!await scope.CanView(product))
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
        // vendorId is forced inside PrepareProducts itself (scope.DefaultVendorId ?? model.SearchVendorId),
        // but storeId is not - every other caller of PrepareProductsModel/PrepareProducts in this file
        // forces it here, at the call site, the same way ProductList does above. This one was missing it:
        // a Store-scoped user could POST an arbitrary (or blank) SearchStoreId and export every store's
        // products, not just their own. Found during a post-Phase-2 tenant-isolation audit.
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;

        var products = await productViewModelService.PrepareProducts(model);
        try
        {
            var bytes = await exportManager.Export(products, HttpContext.RequestAborted);
            return File(bytes, "text/xls", "products.xlsx");
        }
        catch (Exception exc) when (!IsRequestAborted(exc))
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
            products.AddRange(await productService.GetProductsByIds(ids, access: CatalogAccess.IncludingHidden()));
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

        var bytes = await exportManager.Export(scoped, HttpContext.RequestAborted);
        return File(bytes, "text/xls", "products.xlsx");
    }

    // Global scope only, as for brands, categories and collections: the importer is not store- or
    // vendor-aware.
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
            if (Scope.DefaultStoreId is not null || Scope.DefaultVendorId is not null)
            {
                Error(translationService.GetResource("Admin.AccessDenied.Title"));
                return RedirectToAction("List");
            }

            if (importexcelfile is { Length: > 0 })
            {
                await importManager.Import(importexcelfile.OpenReadStream(), HttpContext.RequestAborted);
            }
            else
            {
                Error(translationService.GetResource("Admin.Common.UploadFile"));
                return RedirectToAction("List");
            }

            Success(translationService.GetResource("Admin.Catalog.Products.Imported"));
            return RedirectToAction("List");
        }
        catch (Exception exc) when (!IsRequestAborted(exc))
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
        // PrepareBulkEditListModel() now reads scope internally (Task 9): null for Admin/Vendor
        // (not store-scoped), StaffStoreId for Store.
        var model = await productViewModelService.PrepareBulkEditListModel();
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

    /// <summary>
    /// Rows added in the bulk-edit grid: a simple product each, with the fields of the row. The ids
    /// are the grid's empty ones, so there is nothing to scope-check; ownership of the new product
    /// is set by the service (store for Store, vendor for Vendor) the way the Create screen sets it.
    /// </summary>
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    public async Task<IActionResult> BulkEditCreate(IEnumerable<BulkEditProductModel> products)
    {
        var newProducts = products?.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList() ?? [];
        if (newProducts.Count > 0) await productViewModelService.InsertBulkEdit(newProducts);

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

        var loadedProducts = await productService.GetProductsByIds(byId.Keys.ToArray(),
            access: CatalogAccess.IncludingHidden());
        var validProducts = new List<BulkEditProductModel>();
        foreach (var product in loadedProducts)
            if (await scope.HasAccess(product))
                validProducts.Add(byId[product.Id]);

        return validProducts;
    }

    #endregion

    #region Product attributes

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductAttributeMappingList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess applied uniformly across this region (Admin's Global scope is a no-op). Store originally
        // checked ownership here (CanAccessProduct) and Vendor did too (CheckAccessToProduct), but neither
        // host checked ProductAttributeMappingPopup(POST) or ProductAttributeValidationRulesPopup(POST) at
        // all - a store/vendor user could edit an attribute mapping's name/values or its validation rules on
        // any product (not just their own/in-scope one) by posting a known productId. Vendor's
        // ProductAttributeMappingPopup(POST) had no check either, despite its own GET sibling checking.
        if (!await scope.CanView(product))
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
        // PrepareAssociateProductToAttributeValueModel() now reads scope internally (Task 9):
        // StaffStoreId for Store; null for Admin/Vendor (no store concept).
        var model =
            await productViewModelService.PrepareAssociateProductToAttributeValueModel();
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

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess added here: Admin's original had no ownership check on this grid at all. Store's
        // original used CanAccessProduct + a hardcoded "Admin.Catalog.Products.Permissions" resource key
        // (even though it's the Store host); Vendor's original used a local CheckAccessToProduct helper
        // that returned a plain hardcoded string ("This is not your product" / "Product not exists")
        // rather than a resource key. scope.HasAccess plus the scope.ResourceKeyPrefix-qualified resource
        // key normalizes all three to the convention used throughout this file.
        if (!await scope.CanView(product))
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
}
