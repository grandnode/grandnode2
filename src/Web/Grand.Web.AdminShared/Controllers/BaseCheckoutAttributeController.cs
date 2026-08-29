using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.CheckoutAttributes)]
public abstract class BaseCheckoutAttributeController(
    ICheckoutAttributeService checkoutAttributeService,
    ILanguageService languageService,
    ITranslationService translationService,
    ICurrencyService currencyService,
    CurrencySettings currencySettings,
    IMeasureService measureService,
    MeasureSettings measureSettings,
    ICheckoutAttributeViewModelService checkoutAttributeViewModelService,
    IAdminDataScope<CheckoutAttribute> scope) : BaseController
{
    #region Checkout attributes

    public virtual IActionResult Index() => RedirectToAction("List");
    public virtual IActionResult List() => View();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public virtual async Task<IActionResult> List(DataSourceRequest command)
    {
        var checkoutAttributes = await checkoutAttributeViewModelService.PrepareCheckoutAttributeListModel(scope.DefaultStoreId);
        return Json(new DataSourceResult { Data = checkoutAttributes.ToList(), Total = checkoutAttributes.Count() });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public virtual async Task<IActionResult> Create()
    {
        var model = await checkoutAttributeViewModelService.PrepareCheckoutAttributeModel();
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public virtual async Task<IActionResult> Create(CheckoutAttributeModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null)
            {
                model.Stores = [scope.DefaultStoreId];
                model.CustomerGroups = [];
            }
            var checkoutAttribute = await checkoutAttributeViewModelService.InsertCheckoutAttributeModel(model);
            Success(translationService.GetResource("Admin.Orders.CheckoutAttributes.Added"));
            return continueEditing
                ? RedirectToAction("Edit", new { id = checkoutAttribute.Id })
                : RedirectToAction("List");
        }
        await checkoutAttributeViewModelService.PrepareTaxCategories(model, null, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public virtual async Task<IActionResult> Edit(string id)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(id);
        if (checkoutAttribute == null || !await scope.CanView(checkoutAttribute))
            return RedirectToAction("List");

        var model = checkoutAttribute.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = checkoutAttribute.GetTranslation(x => x.Name, languageId, false);
            locale.TextPrompt = checkoutAttribute.GetTranslation(x => x.TextPrompt, languageId, false);
        });
        await checkoutAttributeViewModelService.PrepareTaxCategories(model, checkoutAttribute, false);
        await checkoutAttributeViewModelService.PrepareConditionAttributes(model, checkoutAttribute);
        return View(model);
    }

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> Edit(CheckoutAttributeModel model, bool continueEditing)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(model.Id);
        if (checkoutAttribute == null || !await scope.HasAccess(checkoutAttribute))
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null)
            {
                model.Stores = [scope.DefaultStoreId];
                model.CustomerGroups = [];
            }
            checkoutAttribute = await checkoutAttributeViewModelService.UpdateCheckoutAttributeModel(checkoutAttribute, model);
            Success(translationService.GetResource("Admin.Orders.CheckoutAttributes.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = checkoutAttribute.Id });
            }
            return RedirectToAction("List");
        }
        await checkoutAttributeViewModelService.PrepareTaxCategories(model, checkoutAttribute, true);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public virtual async Task<IActionResult> Delete(string id)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(id);
        if (checkoutAttribute == null || !await scope.HasAccess(checkoutAttribute))
            return RedirectToAction("List");

        await checkoutAttributeService.DeleteCheckoutAttribute(checkoutAttribute);
        Success(translationService.GetResource("Admin.Orders.CheckoutAttributes.Deleted"));
        return RedirectToAction("List");
    }

    #endregion

    #region Checkout attribute values

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public virtual async Task<IActionResult> ValueList(string checkoutAttributeId, DataSourceRequest command)
    {
        var ca = await checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
        if (!await scope.CanView(ca))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.checkoutAttributes.permissions"));

        var values = await checkoutAttributeViewModelService.PrepareCheckoutAttributeValuesModel(checkoutAttributeId);
        return Json(new DataSourceResult { Data = values.ToList(), Total = values.Count() });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueCreatePopup(string checkoutAttributeId)
    {
        var ca = await checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
        if (ca == null)
            throw new ArgumentException("No checkout attribute found with the specified id");
        if (!await scope.HasAccess(ca))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.checkoutattributes.permissions"));

        var model = await checkoutAttributeViewModelService.PrepareCheckoutAttributeValueModel(checkoutAttributeId);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueCreatePopup(CheckoutAttributeValueModel model)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(model.CheckoutAttributeId);
        if (checkoutAttribute == null)
            return RedirectToAction("List");
        if (!await scope.HasAccess(checkoutAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.checkoutattributes.permissions"));

        if (ModelState.IsValid)
        {
            await checkoutAttributeViewModelService.InsertCheckoutAttributeValueModel(checkoutAttribute, model);
            return Content("");
        }
        model.PrimaryStoreCurrencyCode = (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId)).CurrencyCode;
        model.BaseWeightIn = (await measureService.GetMeasureWeightById(measureSettings.BaseWeightId)).Name;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueEditPopup(string id, string checkoutAttributeId)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
        if (!await scope.CanView(checkoutAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.checkoutAttributes.permissions"));

        var cav = checkoutAttribute.CheckoutAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            return RedirectToAction("List");

        var model = await checkoutAttributeViewModelService.PrepareCheckoutAttributeValueModel(checkoutAttribute, cav);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = cav.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueEditPopup(CheckoutAttributeValueModel model)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(model.CheckoutAttributeId);
        if (!await scope.HasAccess(checkoutAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.checkoutAttributes.permissions"));

        var cav = checkoutAttribute.CheckoutAttributeValues.FirstOrDefault(x => x.Id == model.Id);
        if (cav == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await checkoutAttributeViewModelService.UpdateCheckoutAttributeValueModel(checkoutAttribute, cav, model);
            return Content("");
        }
        model.PrimaryStoreCurrencyCode = (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId)).CurrencyCode;
        model.BaseWeightIn = (await measureService.GetMeasureWeightById(measureSettings.BaseWeightId)).Name;
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> ValueDelete(string id, string checkoutAttributeId)
    {
        var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(checkoutAttributeId);
        if (!await scope.HasAccess(checkoutAttribute))
            return View("AccessDenied", translationService.GetResource("admin.Catalog.attributes.checkoutAttributes.permissions"));

        var cav = checkoutAttribute.CheckoutAttributeValues.FirstOrDefault(x => x.Id == id);
        if (cav == null)
            throw new ArgumentException("No checkout attribute value found with the specified id");

        if (ModelState.IsValid)
        {
            checkoutAttribute.CheckoutAttributeValues.Remove(cav);
            await checkoutAttributeService.UpdateCheckoutAttribute(checkoutAttribute);
            return new JsonResult("");
        }
        return ErrorForKendoGridJson(ModelState);
    }

    #endregion
}
