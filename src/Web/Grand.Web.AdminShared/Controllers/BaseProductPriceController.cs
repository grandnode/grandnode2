using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductPriceController(
    IProductPriceViewModelService productPriceViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IDateTimeService dateTimeService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Product currency price

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductPriceList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict) mirrors Store's CanAccessProduct (AccessToEntityByStore) check
        // on this action. Applying it uniformly also closes real gaps on the mutate actions below: Store's
        // original checked access only on List/Insert (ProductPriceUpdate/ProductPriceDelete had no check
        // at all), and Vendor's original checked access only on List (ProductPriceInsert/Update/Delete had
        // no check at all) - both let another party's product prices be updated/deleted by id.
        if (!await scope.CanView(product))
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

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess on List (Store/Vendor both checked here; Admin's Global scope is a no-op). Closes the
        // same class of gap as "Product currency price": Vendor's original checked ownership only on List
        // and TierPriceEditPopup(GET) - TierPriceCreatePopup(POST), TierPriceEditPopup(POST) and
        // TierPriceDelete had NO ownership check at all, so a vendor could create/update/delete a tier
        // price on any product (not just their own) by posting a known productId. Applying scope.HasAccess
        // uniformly on every mutating action below closes that.
        if (!await scope.CanView(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        // PrepareTierPriceModel(product) now reads scope internally (Task 9); scope.DefaultStoreId
        // is null for Admin/Vendor (Global/VendorProduct scopes) and the staff store for Store.
        var tierPricesModel = await productPriceViewModelService.PrepareTierPriceModel(product);
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
        await productPriceViewModelService.PrepareTierPriceModel(model);
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
        await productPriceViewModelService.PrepareTierPriceModel(model);
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
        await productPriceViewModelService.PrepareTierPriceModel(model);
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
        await productPriceViewModelService.PrepareTierPriceModel(model);
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
}
