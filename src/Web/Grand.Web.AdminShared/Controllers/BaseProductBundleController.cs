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
public abstract class BaseProductBundleController(
    IProductAddPopupViewModelService productAddPopupViewModelService,
    IProductBundleViewModelService productBundleViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Bundle products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> BundleProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict) has the same shape as "Related products"/"Similar products" above -
        // mirrors Store's CanAccessProduct check on this action. Applying it uniformly also closes the
        // same kind of gap found in those two regions: Vendor's original BundleProductUpdate/Delete/
        // AddPopup(GET/POST) (below) had no ownership check at all - only List checked (via
        // CheckAccessToProduct) - letting any vendor mutate another vendor's bundle-product mappings by id.
        if (!await scope.CanView(product))
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
            await productBundleViewModelService.UpdateBundleProductModel(model);
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
            await productBundleViewModelService.DeleteBundleProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> BundleProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. PrepareBundleProductModel() now reads scope
        // internally (Task 9).
        var model = await productAddPopupViewModelService.PrepareBundleProductModel();
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
            await productAddPopupViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
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
            if (model.SelectedProductIds != null) await productBundleViewModelService.InsertBundleProductModel(model);
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
        model = await productAddPopupViewModelService.PrepareBundleProductModel();
        return View(model);
    }

    #endregion
}
