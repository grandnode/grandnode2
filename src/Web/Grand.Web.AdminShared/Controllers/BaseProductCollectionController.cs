using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
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
public abstract class BaseProductCollectionController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Product collections

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductCollectionList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict) has the same shape as "Product categories" above - mirrors Store's
        // CanAccessProduct and Vendor's CheckAccessToProduct gating this action on both hosts. Applying
        // it uniformly also closes the same kind of gap found in "Product categories": Store's and
        // Vendor's original ProductCollectionInsert/Update/Delete (below) had no ownership check at all
        // - only List checked - letting any store manager or vendor mutate another party's
        // product-collection mappings by id.
        if (!await scope.CanView(product))
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
}
