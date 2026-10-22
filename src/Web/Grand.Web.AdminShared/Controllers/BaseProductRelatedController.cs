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
public abstract class BaseProductRelatedController(
    IProductAddPopupViewModelService productAddPopupViewModelService,
    IProductRelatedViewModelService productRelatedViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Related products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> RelatedProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict) has the same shape as "Product categories"/"Product collections" -
        // mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating this action on both
        // hosts. Applying it uniformly also closes the same kind of gap found in those two regions:
        // Vendor's original RelatedProductUpdate/Delete/AddPopup(POST) (below) had no ownership check at
        // all - only List checked - letting any vendor mutate another vendor's related-product mappings
        // by id.
        if (!await scope.CanView(product))
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
            await productRelatedViewModelService.UpdateRelatedProductModel(model);
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
            await productRelatedViewModelService.DeleteRelatedProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RelatedProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. PrepareRelatedProductModel() now reads scope
        // internally (Task 9).
        var model = await productAddPopupViewModelService.PrepareRelatedProductModel();
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
            await productAddPopupViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
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
            if (model.SelectedProductIds != null) await productRelatedViewModelService.InsertRelatedProductModel(model);
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
        model = await productAddPopupViewModelService.PrepareRelatedProductModel();
        return View(model);
    }

    #endregion

    #region Similar products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> SimilarProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict) has the same shape as "Related products" above - mirrors Store's
        // CanAccessProduct check on this action. Applying it uniformly also closes a real gap: Vendor's
        // original SimilarProductUpdate/Delete/AddPopup(GET/POST) (below) had no ownership check at all -
        // only List checked (via CheckAccessToProduct) - letting any vendor mutate another vendor's
        // similar-product mappings by id.
        if (!await scope.CanView(product))
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
            await productRelatedViewModelService.UpdateSimilarProductModel(model);
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
            await productRelatedViewModelService.DeleteSimilarProductModel(model);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SimilarProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. PrepareSimilarProductModel() now reads scope
        // internally (Task 9).
        var model = await productAddPopupViewModelService.PrepareSimilarProductModel();
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
            await productAddPopupViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
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
            if (model.SelectedProductIds != null) await productRelatedViewModelService.InsertSimilarProductModel(model);
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
        model = await productAddPopupViewModelService.PrepareSimilarProductModel();
        return View(model);
    }

    #endregion

    #region Cross-sell products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CrossSellProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict) has the same shape as "Related products"/"Bundle products" above -
        // mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating this action on both
        // hosts. Admin's original CrossSellProductList had no check at all - applying HasAccess uniformly
        // also closes that gap without changing Admin's superuser behaviour (HasAccess is a no-op for
        // Admin's scope).
        if (!await scope.CanView(product))
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
            await productRelatedViewModelService.DeleteCrossSellProduct(product.Id, crossSellProduct);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CrossSellProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. PrepareCrossSellProductModel() now reads scope
        // internally (Task 9).
        var model = await productAddPopupViewModelService.PrepareCrossSellProductModel();
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
            await productAddPopupViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
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
            if (model.SelectedProductIds != null) await productRelatedViewModelService.InsertCrossSellProductModel(model);
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
        model = await productAddPopupViewModelService.PrepareCrossSellProductModel();
        return View(model);
    }

    #endregion

    #region Recommended products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> RecommendedProductList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict) has the same shape as "Cross-sell products" above - mirrors Store's
        // CanAccessProduct and Vendor's CheckAccessToProduct gating this action on both hosts. Admin's
        // original RecommendedProductList had no check at all - applying HasAccess uniformly also closes
        // that gap without changing Admin's superuser behaviour (HasAccess is a no-op for Admin's scope).
        // Vendor's original signature also dropped the DataSourceRequest command parameter entirely
        // (unused by the body on any host either way) - kept here for parity with Admin/Store.
        if (!await scope.CanView(product))
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
            await productRelatedViewModelService.DeleteRecommendedProduct(product.Id, recommendedProduct);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RecommendedProductAddPopup(string productId)
    {
        // No access check here in any of the three original hosts (Admin/Store/Vendor all open this
        // popup unconditionally once the Edit permission is satisfied) - only the mutating POST below
        // ties access to a specific product. PrepareRecommendedProductModel() now reads scope
        // internally (Task 9).
        var model = await productAddPopupViewModelService.PrepareRecommendedProductModel();
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
            await productAddPopupViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
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
            if (model.SelectedProductIds != null) await productRelatedViewModelService.InsertRecommendedProductModel(model);
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
        model = await productAddPopupViewModelService.PrepareRecommendedProductModel();
        return View(model);
    }

    #endregion
}
