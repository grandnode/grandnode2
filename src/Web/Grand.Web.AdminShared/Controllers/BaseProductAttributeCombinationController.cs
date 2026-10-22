using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductAttributeCombinationController(
    IProductAttributeCombinationViewModelService productAttributeCombinationViewModelService,
    IProductService productService,
    IInventoryManageService inventoryManageService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
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

        var combinationsModel = await productAttributeCombinationViewModelService.PrepareProductAttributeCombinationModel(product);
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

        var model = await productAttributeCombinationViewModelService.PrepareProductAttributeCombinationModel(product, id);
        await productAttributeCombinationViewModelService.PrepareAddProductAttributeCombinationModel(model, product);
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

        var warnings = await productAttributeCombinationViewModelService.InsertOrUpdateProductAttributeCombinationPopup(product, model);
        if (!warnings.Any()) return Content("");
        //If we got this far, something failed, redisplay form
        await productAttributeCombinationViewModelService.PrepareAddProductAttributeCombinationModel(model, product);
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

        await productAttributeCombinationViewModelService.GenerateAllAttributeCombinations(product);

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
            await productAttributeCombinationViewModelService.ClearAllAttributeCombinations(product);

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
            await productAttributeCombinationViewModelService.PrepareProductAttributeCombinationTierPricesModel(product,
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
            await productAttributeCombinationViewModelService.InsertProductAttributeCombinationTierPricesModel(product, combination,
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
            await productAttributeCombinationViewModelService.UpdateProductAttributeCombinationTierPricesModel(product, combination,
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
                await productAttributeCombinationViewModelService.DeleteProductAttributeCombinationTierPrices(product, combination,
                    tierPrice);
        }

        return new JsonResult("");
    }

    #endregion
}
