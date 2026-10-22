using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
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
public abstract class BaseProductAttributeMappingController(
    IProductAddPopupViewModelService productAddPopupViewModelService,
    IProductAttributeMappingViewModelService productAttributeMappingViewModelService,
    IProductService productService,
    ILanguageService languageService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
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

        var attributesModel = await productAttributeMappingViewModelService.PrepareProductAttributeMappingModels(product);
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
            var model = await productAttributeMappingViewModelService.PrepareProductAttributeMappingModel(product);
            return View(model);
        }

        var productAttributeMapping =
            product.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        var editModel = await productAttributeMappingViewModelService.PrepareProductAttributeMappingModel(product,
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
                await productAttributeMappingViewModelService.InsertProductAttributeMappingModel(model);
            else
                await productAttributeMappingViewModelService.UpdateProductAttributeMappingModel(model);

            return Content("");
        }

        Error(ModelState);
        model = await productAttributeMappingViewModelService.PrepareProductAttributeMappingModel(model);
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

        var model = await productAttributeMappingViewModelService.PrepareProductAttributeMappingModel(productAttributeMapping);
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
            await productAttributeMappingViewModelService.UpdateProductAttributeValidationRulesModel(productAttributeMapping, model);
            return Content("");
        }

        Error(ModelState);
        model = await productAttributeMappingViewModelService.PrepareProductAttributeMappingModel(productAttributeMapping);
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

        var model = await productAttributeMappingViewModelService.PrepareProductAttributeConditionModel(product,
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

        await productAttributeMappingViewModelService.UpdateProductAttributeConditionModel(product, productAttributeMapping, model);
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
            await productAttributeMappingViewModelService.PrepareProductAttributeValueModels(product, productAttributeMapping);
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
            await productAttributeMappingViewModelService.PrepareProductAttributeValueModel(product, productAttributeMapping);
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
            await productAttributeMappingViewModelService.InsertProductAttributeValueModel(model);
            return Content("");
        }

        //If we got this far, something failed, redisplay form
        await productAttributeMappingViewModelService.PrepareProductAttributeValueModel(product, model);
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

        var model = await productAttributeMappingViewModelService.PrepareProductAttributeValueModel(pa, pav);
        //locales
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = pav.GetTranslation(x => x.Name, languageId, false);
        });
        //pictures
        await productAttributeMappingViewModelService.PrepareProductAttributeValueModel(product, model);
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
            await productAttributeMappingViewModelService.UpdateProductAttributeValueModel(pav, model);
            return Content("");
        }

        //If we got this far, something failed, redisplay form
        await productAttributeMappingViewModelService.PrepareProductAttributeValueModel(product, model);
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
            await productAddPopupViewModelService.PrepareAssociateProductToAttributeValueModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AssociateProductToAttributeValuePopupList(DataSourceRequest command,
        ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel model)
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
}
