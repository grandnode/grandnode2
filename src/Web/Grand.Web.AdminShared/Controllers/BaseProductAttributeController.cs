using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.ProductAttributes)]
[AutoValidateAntiforgeryToken]
public abstract class BaseProductAttributeController(
    IProductService productService,
    IProductAttributeService productAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    SeoSettings seoSettings,
    IAdminDataScope<ProductAttribute> scope) : BaseController
{
    #region Attribute list / create / edit / delete

    public IActionResult Index() => RedirectToAction("List");
    public IActionResult List() => View();

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var productAttributes = await productAttributeService.GetAllProductAttributes(
            scope.DefaultStoreId ?? "", command.Page - 1, command.PageSize);
        return Json(new DataSourceResult {
            Data = productAttributes.Select(x => x.ToModel()),
            Total = productAttributes.TotalCount
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new ProductAttributeModel();
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(ProductAttributeModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var productAttribute = model.ToEntity();
            productAttribute.SeName = SeoExtensions.GetSeName(
                string.IsNullOrEmpty(productAttribute.SeName) ? productAttribute.Name : productAttribute.SeName,
                seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);
            await productAttributeService.InsertProductAttribute(productAttribute);
            Success(translationService.GetResource("Admin.Catalog.Attributes.ProductAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = productAttribute.Id })
                : RedirectToAction("List");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(id);
        if (productAttribute == null || !await scope.CanView(productAttribute))
            return RedirectToAction("List");

        var model = productAttribute.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = productAttribute.GetTranslation(x => x.Name, languageId, false);
            locale.Description = productAttribute.GetTranslation(x => x.Description, languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(ProductAttributeModel model, bool continueEditing)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(model.Id);
        if (productAttribute == null || !await scope.HasAccess(productAttribute))
            return RedirectToAction(productAttribute == null ? "List" : "Edit", new { id = model.Id });

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            productAttribute = model.ToEntity(productAttribute);
            productAttribute.SeName = SeoExtensions.GetSeName(
                string.IsNullOrEmpty(productAttribute.SeName) ? productAttribute.Name : productAttribute.SeName,
                seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);
            await productAttributeService.UpdateProductAttribute(productAttribute);
            Success(translationService.GetResource("Admin.Catalog.Attributes.ProductAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = productAttribute.Id });
            }
            return RedirectToAction("List");
        }

        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = productAttribute.GetTranslation(x => x.Name, languageId, false);
            locale.Description = productAttribute.GetTranslation(x => x.Description, languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(id);
        if (productAttribute == null)
            return RedirectToAction("List");
        if (!await scope.HasAccess(productAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await productAttributeService.DeleteProductAttribute(productAttribute);
            Success(translationService.GetResource("Admin.Catalog.Attributes.ProductAttributes.Deleted"));
            return RedirectToAction("List");
        }
        Error(ModelState);
        return RedirectToAction("Edit", new { id = productAttribute.Id });
    }

    #endregion

    #region Used by products

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> UsedByProducts(DataSourceRequest command, string productAttributeId)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(productAttributeId);
        if (!await scope.CanView(productAttribute))
            return RedirectToAction("List");

        var products = await productService.GetProductsByProductAttributeId(
            productAttributeId, scope.DefaultStoreId ?? "", command.Page - 1, command.PageSize);
        return Json(new DataSourceResult {
            Data = products.Select(x => new ProductAttributeModel.UsedByProductModel {
                Id = x.Id, ProductName = x.Name, Published = x.Published
            }),
            Total = products.TotalCount
        });
    }

    #endregion

    #region Predefined values

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> PredefinedProductAttributeValueList(string productAttributeId, DataSourceRequest command)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(productAttributeId);
        if (!await scope.CanView(productAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.productattributes.permissions"));

        var values = productAttribute.PredefinedProductAttributeValues;
        return Json(new DataSourceResult { Data = values.Select(x => x.ToModel()), Total = values.Count });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> PredefinedProductAttributeValueCreatePopup(string productAttributeId)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(productAttributeId);
        if (productAttribute == null)
            throw new ArgumentException("No product attribute found with the specified id");
        if (!await scope.HasAccess(productAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.productattributes.permissions"));

        var model = new PredefinedProductAttributeValueModel { ProductAttributeId = productAttributeId };
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PredefinedProductAttributeValueCreatePopup(PredefinedProductAttributeValueModel model)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(model.ProductAttributeId);
        if (productAttribute == null)
            throw new ArgumentException("No product attribute found with the specified id");
        if (!await scope.HasAccess(productAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.productattributes.permissions"));

        if (ModelState.IsValid)
        {
            var ppav = model.ToEntity();
            productAttribute.PredefinedProductAttributeValues.Add(ppav);
            await productAttributeService.UpdateProductAttribute(productAttribute);
            return Content("");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> PredefinedProductAttributeValueEditPopup(string id, string productAttributeId)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(productAttributeId);
        if (!await scope.CanView(productAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.productattributes.permissions"));

        var ppav = productAttribute.PredefinedProductAttributeValues.FirstOrDefault(x => x.Id == id);
        if (ppav == null)
            throw new ArgumentException("No product attribute value found with the specified id");

        var model = ppav.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = ppav.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PredefinedProductAttributeValueEditPopup(PredefinedProductAttributeValueModel model)
    {
        var productAttribute = await productAttributeService.GetProductAttributeById(model.ProductAttributeId);
        if (!await scope.HasAccess(productAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.productattributes.permissions"));

        var ppav = productAttribute.PredefinedProductAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (ppav == null)
            throw new ArgumentException("No product attribute value found with the specified id");

        if (ModelState.IsValid)
        {
            ppav = model.ToEntity(ppav);
            await productAttributeService.UpdateProductAttribute(productAttribute);
            return Content("");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> PredefinedProductAttributeValueDelete(string id)
    {
        var productAttributes = await productAttributeService.GetAllProductAttributes(scope.DefaultStoreId ?? "");
        var productAttribute = productAttributes.FirstOrDefault(x => x.PredefinedProductAttributeValues.Any(p => p.Id == id));
        if (productAttribute == null)
            throw new ArgumentException("No product attribute found with the specified id");
        if (!await scope.HasAccess(productAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.productattributes.permissions"));

        if (ModelState.IsValid)
        {
            var ppav = productAttribute.PredefinedProductAttributeValues.FirstOrDefault(x => x.Id == id);
            if (ppav == null)
                throw new ArgumentException("No predefined product attribute value found with the specified id");
            productAttribute.PredefinedProductAttributeValues.Remove(ppav);
            await productAttributeService.UpdateProductAttribute(productAttribute);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
