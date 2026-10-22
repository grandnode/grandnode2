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
public abstract class BaseProductCategoryController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Product categories

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductCategoryList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict) mirrors Store's CanAccessProduct (AccessToEntityByStore) and
        // Vendor's CheckAccessToProduct (VendorId equality) - both strict rules, both gate this same
        // action on their respective hosts. Applying it uniformly also closes a real gap: Vendor's
        // original ProductCategoryInsert/Update/Delete (below) had no ownership check at all, letting
        // any vendor mutate another vendor's product-category mappings by id.
        if (!await scope.CanView(product))
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
}
