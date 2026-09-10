using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

[SharedViewFolder("Shipping")]
public abstract class BaseShippingMethodController(
    IShippingMethodService shippingMethodService,
    ILanguageService languageService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<ShippingMethod> scope) : BaseController
{
    protected virtual async Task PrepareShippingMethodModel(ShippingMethodModel model)
    {
        if (scope.DefaultStoreId is null)
        {
            model.AvailableStores.Add(new SelectListItem {
                Text = translationService.GetResource("Admin.Configuration.Shipping.Methods.SelectStore"),
                Value = ""
            });
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id, Selected = s.Id == model.StoreId });
        }
    }

    protected virtual async Task<IActionResult> ShippingMethodListCore()
    {
        var storeMap = (await storeService.GetAllStores()).ToDictionary(s => s.Id, s => s.Shortcut);
        var methods = await shippingMethodService.GetAllShippingMethods(storeId: scope.DefaultStoreId ?? "");
        var model = methods.Select(x => {
            var m = x.ToModel();
            m.StoreName = !string.IsNullOrEmpty(x.StoreId) && storeMap.TryGetValue(x.StoreId, out var name) ? name : "";
            return m;
        }).ToList();
        return Json(new DataSourceResult { Data = model, Total = model.Count });
    }

    protected virtual async Task<IActionResult> ShippingMethodCreateGetCore()
    {
        var model = new ShippingMethodModel();
        await AddLocales(languageService, model.Locales);
        await PrepareShippingMethodModel(model);
        return View(model);
    }

    protected virtual async Task<IActionResult> ShippingMethodCreatePostCore(ShippingMethodModel model, bool continueEditing)
    {
        if (!ModelState.IsValid)
        {
            await PrepareShippingMethodModel(model);
            return View(model);
        }
        var sm = model.ToEntity();
        if (scope.DefaultStoreId is not null) sm.StoreId = scope.DefaultStoreId;
        await shippingMethodService.InsertShippingMethod(sm);
        Success(translationService.GetResource("Admin.Configuration.Shipping.Methods.Added"));
        return continueEditing ? RedirectToAction("EditMethod", new { id = sm.Id }) : RedirectToAction("Methods");
    }

    protected virtual async Task<IActionResult> ShippingMethodEditGetCore(string id)
    {
        var sm = await shippingMethodService.GetShippingMethodById(id);
        if (sm == null || !await scope.HasAccess(sm)) return RedirectToAction("Methods");
        var model = sm.ToModel();
        await AddLocales(languageService, model.Locales, (locale, languageId) => {
            locale.Name = sm.GetTranslation(x => x.Name, languageId, false);
            locale.Description = sm.GetTranslation(x => x.Description, languageId, false);
        });
        await PrepareShippingMethodModel(model);
        return View(model);
    }

    protected virtual async Task<IActionResult> ShippingMethodEditPostCore(ShippingMethodModel model, bool continueEditing)
    {
        var sm = await shippingMethodService.GetShippingMethodById(model.Id);
        if (sm == null || !await scope.HasAccess(sm)) return RedirectToAction("Methods");
        if (!ModelState.IsValid)
        {
            await PrepareShippingMethodModel(model);
            return View(model);
        }
        sm = model.ToEntity(sm);
        if (scope.DefaultStoreId is not null) sm.StoreId = scope.DefaultStoreId;
        await shippingMethodService.UpdateShippingMethod(sm);
        Success(translationService.GetResource("Admin.Configuration.Shipping.Methods.Updated"));
        return continueEditing ? RedirectToAction("EditMethod", new { id = sm.Id }) : RedirectToAction("Methods");
    }

    protected virtual async Task<IActionResult> ShippingMethodDeleteCore(string id)
    {
        var sm = await shippingMethodService.GetShippingMethodById(id);
        if (sm == null || !await scope.HasAccess(sm)) return RedirectToAction("Methods");
        await shippingMethodService.DeleteShippingMethod(sm);
        Success(translationService.GetResource("Admin.Configuration.Shipping.Methods.Deleted"));
        return RedirectToAction("Methods");
    }
}
