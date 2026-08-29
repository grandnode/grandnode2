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

[PermissionAuthorize(PermissionSystemName.SpecificationAttributes)]
public abstract class BaseSpecificationAttributeController(
    ISpecificationAttributeService specificationAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IProductService productService,
    SeoSettings seoSettings,
    IAdminDataScope<SpecificationAttribute> scope) : BaseController
{
    #region Used by products

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> UsedByProducts(DataSourceRequest command, string specificationAttributeId)
    {
        var specification = await specificationAttributeService.GetSpecificationAttributeById(specificationAttributeId);
        if (specification == null)
            throw new ArgumentException("No specification found with the specified id");
        if (!await scope.CanView(specification))
            return RedirectToAction("List");

        var specificationProducts = new List<SpecificationAttributeModel.UsedByProductModel>();
        var total = 0;
        var optionIds = specification.SpecificationAttributeOptions.Select(x => x.Id).ToList();
        if (optionIds.Count > 0)
        {
            var products = (await productService.SearchProducts(
                storeId: scope.DefaultStoreId ?? "",
                specificationOptions: optionIds,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize,
                showHidden: true)).products;

            total = products.TotalCount;
            foreach (var item in products)
            {
                var specOption = item.ProductSpecificationAttributes
                    .FirstOrDefault(x => x.SpecificationAttributeId == specificationAttributeId);
                specificationProducts.Add(new SpecificationAttributeModel.UsedByProductModel {
                    Id = item.Id, ProductName = item.Name,
                    OptionName = specification.SpecificationAttributeOptions
                        .FirstOrDefault(x => x.Id == specOption?.SpecificationAttributeOptionId)?.Name,
                    Published = item.Published
                });
            }
        }
        return Json(new DataSourceResult { Data = specificationProducts, Total = total });
    }

    #endregion

    #region Specification attributes

    public IActionResult Index() => RedirectToAction("List");
    public IActionResult List() => View();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var specificationAttributes = await specificationAttributeService.GetSpecificationAttributes(
            scope.DefaultStoreId ?? "", command.Page - 1, command.PageSize);
        return Json(new DataSourceResult {
            Data = specificationAttributes.Select(x => x.ToModel()),
            Total = specificationAttributes.TotalCount
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new SpecificationAttributeModel();
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(SpecificationAttributeModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            var specificationAttribute = model.ToEntity();
            specificationAttribute.SeName = SeoExtensions.GetSeName(
                string.IsNullOrEmpty(specificationAttribute.SeName) ? specificationAttribute.Name : specificationAttribute.SeName,
                seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);
            await specificationAttributeService.InsertSpecificationAttribute(specificationAttribute);
            Success(translationService.GetResource("Admin.Catalog.Attributes.SpecificationAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = specificationAttribute.Id })
                : RedirectToAction("List");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeById(id);
        if (specificationAttribute == null || !await scope.CanView(specificationAttribute))
            return RedirectToAction("List");

        var model = specificationAttribute.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = specificationAttribute.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(SpecificationAttributeModel model, bool continueEditing)
    {
        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeById(model.Id);
        if (specificationAttribute == null || !await scope.HasAccess(specificationAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null) model.Stores = [scope.DefaultStoreId];
            specificationAttribute = model.ToEntity(specificationAttribute);
            specificationAttribute.SeName = SeoExtensions.GetSeName(
                string.IsNullOrEmpty(specificationAttribute.SeName) ? specificationAttribute.Name : specificationAttribute.SeName,
                seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);
            await specificationAttributeService.UpdateSpecificationAttribute(specificationAttribute);
            Success(translationService.GetResource("Admin.Catalog.Attributes.SpecificationAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = specificationAttribute.Id });
            }
            return RedirectToAction("List");
        }

        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = specificationAttribute.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeById(id);
        if (specificationAttribute == null || !await scope.HasAccess(specificationAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await specificationAttributeService.DeleteSpecificationAttribute(specificationAttribute);
            Success(translationService.GetResource("Admin.Catalog.Attributes.SpecificationAttributes.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id = specificationAttribute.Id });
    }

    #endregion

    #region Specification attribute options

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> OptionList(string specificationAttributeId, DataSourceRequest command)
    {
        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeById(specificationAttributeId);
        if (!await scope.CanView(specificationAttribute))
            return Json("");

        var options = specificationAttribute.SpecificationAttributeOptions.OrderBy(x => x.DisplayOrder);
        return Json(new DataSourceResult {
            Data = options.Select(x =>
            {
                var model = x.ToModel();
                model.NumberOfAssociatedProducts = specificationAttributeService.GetProductSpecificationAttributeCount("", x.Id);
                return model;
            }),
            Total = options.Count()
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> OptionCreatePopup(string specificationAttributeId)
    {
        var model = new SpecificationAttributeOptionModel { SpecificationAttributeId = specificationAttributeId };
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> OptionCreatePopup(SpecificationAttributeOptionModel model)
    {
        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeById(model.SpecificationAttributeId);
        if (specificationAttribute == null)
            return RedirectToAction("List");
        if (!await scope.HasAccess(specificationAttribute))
            return View("AccessDenied", translationService.GetResource("admin.catalog.attributes.specificationattributes.permissions"));

        if (ModelState.IsValid)
        {
            var sao = model.ToEntity();
            sao.SeName = SeoExtensions.GetSeName(string.IsNullOrEmpty(sao.SeName) ? sao.Name : sao.SeName,
                seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);
            if (!model.EnableColorSquaresRgb) sao.ColorSquaresRgb = null;

            specificationAttribute.SpecificationAttributeOptions.Add(sao);
            await specificationAttributeService.UpdateSpecificationAttribute(specificationAttribute);
            return Content("");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> OptionEditPopup(string id)
    {
        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeByOptionId(id);
        if (specificationAttribute == null)
            return RedirectToAction("List");
        if (!await scope.CanView(specificationAttribute))
            return View("AccessDenied", translationService.GetResource("admin.catalog.attributes.specificationattributes.permissions"));

        var sao = specificationAttribute.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == id);
        if (sao == null)
            return RedirectToAction("List");

        var model = sao.ToModel();
        model.EnableColorSquaresRgb = !string.IsNullOrEmpty(sao.ColorSquaresRgb);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = sao.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> OptionEditPopup(SpecificationAttributeOptionModel model)
    {
        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeByOptionId(model.Id);
        if (!await scope.HasAccess(specificationAttribute))
            return View("AccessDenied", translationService.GetResource("admin.catalog.attributes.specificationattributes.permissions"));

        var sao = specificationAttribute.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == model.Id);
        if (sao == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            sao = model.ToEntity(sao);
            sao.SeName = SeoExtensions.GetSeName(string.IsNullOrEmpty(sao.SeName) ? sao.Name : sao.SeName,
                seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls, seoSettings.SeoCharConversion);
            if (!model.EnableColorSquaresRgb) sao.ColorSquaresRgb = null;

            await specificationAttributeService.UpdateSpecificationAttribute(specificationAttribute);
            return Content("");
        }

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> OptionDelete(string id)
    {
        var specificationAttribute = await specificationAttributeService.GetSpecificationAttributeByOptionId(id);
        if (!await scope.HasAccess(specificationAttribute))
            return View("AccessDenied", translationService.GetResource("admin.catalog.attributes.specificationattributes.permissions"));

        if (ModelState.IsValid)
        {
            var sao = specificationAttribute.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == id);
            if (sao == null)
                throw new ArgumentException("No specification attribute option found with the specified id");
            await specificationAttributeService.DeleteSpecificationAttributeOption(sao);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
