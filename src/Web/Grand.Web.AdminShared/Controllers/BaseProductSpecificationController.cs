using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Products)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductSpecificationController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    #region Product specification attributes

    //ajax
    [AcceptVerbs("GET")]
    public async Task<IActionResult> GetOptionsByAttributeId(string attributeId,
        [FromServices] ISpecificationAttributeService specificationAttributeService)
    {
        if (string.IsNullOrEmpty(attributeId))
            return Json("");

        var options =
            (await specificationAttributeService.GetSpecificationAttributeById(attributeId))
            .SpecificationAttributeOptions.OrderBy(x => x.DisplayOrder);
        var result = (from o in options
            select new { id = o.Id, name = o.Name }).ToList();
        return Json(result);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrList(DataSourceRequest command, string productId)
    {
        var product = await productService.GetProductById(productId);

        // CanView on this read: a product shared with other stores opens read-only on Store, so its
        // tabs must load; the mutating actions below keep the strict HasAccess. History:
        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct gating
        // this action on both hosts. Admin's original ProductSpecAttrList had no check at all - applying
        // HasAccess uniformly also closes that gap without changing Admin's superuser behaviour (HasAccess
        // is a no-op for Admin's scope). Vendor's original signature also dropped the DataSourceRequest
        // command parameter entirely (unused by the body on any host either way) - kept here for parity
        // with Admin/Store.
        if (!await scope.CanView(product))
            return ErrorForKendoGridJson(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var productrSpecsModel = await productViewModelService.PrepareProductSpecificationAttributeModel(product);
        var gridModel = new DataSourceResult {
            Data = productrSpecsModel,
            Total = productrSpecsModel.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductSpecAttrPopup(
        [FromServices] ISpecificationAttributeService specificationAttributeService,
        string productId, string id)
    {
        var product = await productService.GetProductById(productId);

        // HasAccess (strict): mirrors Store's CanAccessProduct and Vendor's CheckAccessToProduct check on
        // this action. Admin's original ProductSpecAttrPopup(GET) had no check at all - closed the same
        // way as ProductSpecAttrList above.
        if (!await scope.HasAccess(product))
            return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

        var model = new ProductModel.AddProductSpecificationAttributeModel {
            //default specs values
            ShowOnProductPage = true
        };

        if (!string.IsNullOrEmpty(id))
        {
            var specification = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == id);
            if (specification != null) model = specification.ToModel();
        }

        model.AvailableAttributes = await PrepareAvailableAttributes(specificationAttributeService);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrPopup(
        [FromServices] ISpecificationAttributeService specificationAttributeService,
        ProductModel.AddProductSpecificationAttributeModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId);
            if (product == null)
                return Content("Product not exists");

            if (!await scope.HasAccess(product))
                return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == model.Id);
            if (psa == null)
                await productViewModelService.InsertProductSpecificationAttributeModel(model, product);
            else
                await productViewModelService.UpdateProductSpecificationAttributeModel(psa, model);

            return new JsonResult("");
        }

        Error(ModelState);
        model.AvailableAttributes = await PrepareAvailableAttributes(specificationAttributeService);

        return View(model);
    }

    /// <summary>scope.DefaultStoreId ?? "" matches Store's PrepareAvailableAttributes(StaffStoreId) call
    /// and Admin/Vendor's parameterless call (both pass an empty storeId, seeing every specification
    /// attribute regardless of store).</summary>
    private async Task<List<SelectListItem>> PrepareAvailableAttributes(
        ISpecificationAttributeService specificationAttributeService)
    {
        var availableSpecificationAttributes = new List<SelectListItem>();
        foreach (var sa in await specificationAttributeService.GetSpecificationAttributes(scope.DefaultStoreId ?? ""))
            availableSpecificationAttributes.Add(new SelectListItem {
                Text = sa.Name,
                Value = sa.Id
            });
        return availableSpecificationAttributes;
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductSpecAttrDelete(ProductSpecificationAttributeModel model)
    {
        if (ModelState.IsValid)
        {
            var product = await productService.GetProductById(model.ProductId);
            if (product == null)
                return Content("Product not exists");

            // HasAccess (strict): mirrors Store's CanAccessProduct check on this action. Vendor's original
            // ProductSpecAttrDelete had no check at all, letting any vendor delete another vendor's
            // specification attribute mapping by posting its productId/model.Id - closed here the same
            // way as ProductSpecAttrPopup(POST) above.
            if (!await scope.HasAccess(product))
                return Content(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Permissions"));

            var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == model.Id);
            if (psa == null)
                throw new ArgumentException("No specification attribute found with the specified id");

            await productViewModelService.DeleteProductSpecificationAttribute(product, psa);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
