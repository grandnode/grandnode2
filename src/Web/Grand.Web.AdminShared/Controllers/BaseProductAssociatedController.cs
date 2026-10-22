using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductAssociatedController(
    IProductViewModelService productViewModelService,
    IProductAssociatedViewModelService productAssociatedViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Associated products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> AssociatedProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original had no check at all - GlobalAdminDataScope.HasAccess
        // is a no-op there, so this closes that gap the same way as every other row in this task.
        if (!await scope.CanView(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        // AssociatedProductVendorId (hook below): Vendor's original also passed CurrentVendor.Id into
        // GetAssociatedProducts(vendorId:) so a vendor only sees, among a grouped product's full
        // associated-product set, the ones they themselves own. That vendor filter is unconditional in
        // GetAssociatedProducts regardless of showHidden, unlike its storeId parameter (which only applies
        // when showHidden is false - moot here since all three hosts pass showHidden: true).
        var associatedProducts = await productService.GetAssociatedProducts(productId,
            access: CatalogAccess.IncludingHidden(),
            vendorId: AssociatedProductVendorId);
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
            await productAssociatedViewModelService.DeleteAssociatedProduct(product);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AssociatedProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (all open this popup unconditionally
        // once the Edit permission is satisfied) - only the mutating actions below tie access to a
        // specific product. PrepareAssociatedProductModel() now reads scope internally (Task 9).
        var model = await productViewModelService.PrepareAssociatedProductModel();
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
                if (validIds.Count > 0) await productAssociatedViewModelService.InsertAssociatedProductModel(model);
            }

            return Content("");
        }

        // Unlike Related/Similar/Bundle/Cross-sell/Recommended, all three original hosts share the exact
        // same invalid-model-state handling here (Error(ModelState) + re-prepare + View) - Vendor's
        // AssociatedProductAddPopup(POST) does not use the Content(ModelState.GetErrors()) shortcut it
        // uses in those other regions, so no host-specific hook is needed for this action.
        Error(ModelState);
        model = await productViewModelService.PrepareAssociatedProductModel();
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
