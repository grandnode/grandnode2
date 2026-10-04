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
public abstract class BaseDeliveryDateController(
    IDeliveryDateService deliveryDateService,
    ILanguageService languageService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<DeliveryDate> scope) : BaseController
{
    protected virtual async Task PrepareDeliveryDateModel(DeliveryDateModel model)
    {
        if (scope.DefaultStoreId is null)
        {
            model.AvailableStores.Add(new SelectListItem {
                Text = translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.SelectStore"),
                Value = ""
            });
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id, Selected = s.Id == model.StoreId });
        }
    }

    protected virtual async Task<IActionResult> DeliveryDateListCore()
    {
        var storeMap = (await storeService.GetAllStores()).ToDictionary(s => s.Id, s => s.Shortcut);
        var dates = (await deliveryDateService.GetAllDeliveryDates(scope.DefaultStoreId ?? "")).ToList();
        var model = dates.Select(x => {
            var m = x.ToModel();
            m.StoreName = !string.IsNullOrEmpty(x.StoreId) && storeMap.TryGetValue(x.StoreId, out var name) ? name : "";
            return m;
        }).ToList();
        return Json(new DataSourceResult { Data = model, Total = model.Count });
    }

    protected virtual async Task<IActionResult> DeliveryDateCreateGetCore()
    {
        var model = new DeliveryDateModel { ColorSquaresRgb = "#000000" };
        await AddLocales(languageService, model.Locales);
        await PrepareDeliveryDateModel(model);
        return View(model);
    }

    protected virtual async Task<IActionResult> DeliveryDateCreatePostCore(DeliveryDateModel model, bool continueEditing)
    {
        if (!ModelState.IsValid)
        {
            await PrepareDeliveryDateModel(model);
            return View(model);
        }
        var deliveryDate = model.ToEntity();
        if (scope.DefaultStoreId is not null) deliveryDate.StoreId = scope.DefaultStoreId;
        await deliveryDateService.InsertDeliveryDate(deliveryDate);
        Success(translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Added"));
        return continueEditing
            ? RedirectToAction("EditDeliveryDate", new { id = deliveryDate.Id })
            : RedirectToAction("DeliveryDates");
    }

    protected virtual async Task<IActionResult> DeliveryDateEditGetCore(string id)
    {
        var deliveryDate = await deliveryDateService.GetDeliveryDateById(id);
        if (deliveryDate == null || !await scope.HasAccess(deliveryDate)) return RedirectToAction("DeliveryDates");
        var model = deliveryDate.ToModel();
        if (string.IsNullOrEmpty(model.ColorSquaresRgb)) model.ColorSquaresRgb = "#000000";
        await AddLocales(languageService, model.Locales, (locale, languageId) => {
            locale.Name = deliveryDate.GetTranslation(x => x.Name, languageId, false);
        });
        await PrepareDeliveryDateModel(model);
        return View(model);
    }

    protected virtual async Task<IActionResult> DeliveryDateEditPostCore(DeliveryDateModel model, bool continueEditing)
    {
        var deliveryDate = await deliveryDateService.GetDeliveryDateById(model.Id);
        if (deliveryDate == null || !await scope.HasAccess(deliveryDate)) return RedirectToAction("DeliveryDates");
        if (!ModelState.IsValid)
        {
            await PrepareDeliveryDateModel(model);
            return View(model);
        }
        deliveryDate = model.ToEntity(deliveryDate);
        if (scope.DefaultStoreId is not null) deliveryDate.StoreId = scope.DefaultStoreId;
        await deliveryDateService.UpdateDeliveryDate(deliveryDate);
        Success(translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Updated"));
        return continueEditing
            ? RedirectToAction("EditDeliveryDate", new { id = deliveryDate.Id })
            : RedirectToAction("DeliveryDates");
    }

    protected virtual async Task<IActionResult> DeliveryDateDeleteCore(string id)
    {
        var deliveryDate = await deliveryDateService.GetDeliveryDateById(id);
        if (deliveryDate == null || !await scope.HasAccess(deliveryDate)) return RedirectToAction("DeliveryDates");
        await deliveryDateService.DeleteDeliveryDate(deliveryDate);
        Success(translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Deleted"));
        return RedirectToAction("DeliveryDates");
    }
}
