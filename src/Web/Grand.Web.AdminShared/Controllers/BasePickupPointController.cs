using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Directory;
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
public abstract class BasePickupPointController(
    IPickupPointService pickupPointService,
    IWarehouseService warehouseService,
    ICountryService countryService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<PickupPoint> scope) : BaseController
{
    protected virtual async Task PreparePickupPointModel(PickupPointModel model)
    {
        model.Address.AvailableCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.Address.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == model.Address.CountryId });
        var states = !string.IsNullOrEmpty(model.Address.CountryId)
            ? (await countryService.GetCountryById(model.Address.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states?.Count > 0)
            foreach (var s in states)
                model.Address.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == model.Address.StateProvinceId });

        model.Address.CountryEnabled = true;
        model.Address.StateProvinceEnabled = true;
        model.Address.CityEnabled = true;
        model.Address.StreetAddressEnabled = true;
        model.Address.ZipPostalCodeEnabled = true;
        model.Address.ZipPostalCodeRequired = true;
        model.Address.PhoneEnabled = true;
        model.Address.FaxEnabled = true;
        model.Address.CompanyEnabled = true;

        if (scope.DefaultStoreId is null)
        {
            model.AvailableStores.Add(new SelectListItem {
                Text = translationService.GetResource("Admin.Configuration.Shipping.PickupPoint.SelectStore"),
                Value = ""
            });
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id, Selected = s.Id == model.StoreId });
        }

        model.AvailableWarehouses.Add(new SelectListItem {
            Text = translationService.GetResource("Admin.Configuration.Shipping.PickupPoint.SelectWarehouse"),
            Value = ""
        });
        foreach (var w in await warehouseService.GetAllWarehouses(scope.DefaultStoreId ?? ""))
            model.AvailableWarehouses.Add(new SelectListItem { Text = w.Name, Value = w.Id, Selected = w.Id == model.WarehouseId });
    }

    protected virtual async Task<IActionResult> PickupPointListCore()
    {
        var storeMap = (await storeService.GetAllStores()).ToDictionary(s => s.Id, s => s.Shortcut);
        var points = (await pickupPointService.GetAllPickupPoints(scope.DefaultStoreId ?? "")).ToList();
        var model = points.Select(x => {
            var m = x.ToModel();
            m.StoreName = !string.IsNullOrEmpty(x.StoreId) && storeMap.TryGetValue(x.StoreId, out var name) ? name : "";
            return m;
        }).ToList();
        return Json(new DataSourceResult { Data = model, Total = model.Count });
    }

    protected virtual async Task<IActionResult> PickupPointCreateGetCore()
    {
        var model = new PickupPointModel();
        await PreparePickupPointModel(model);
        return View(model);
    }

    protected virtual async Task<IActionResult> PickupPointCreatePostCore(PickupPointModel model, bool continueEditing)
    {
        if (!ModelState.IsValid)
        {
            await PreparePickupPointModel(model);
            return View(model);
        }
        var pickupPoint = model.ToEntity();
        if (scope.DefaultStoreId is not null) pickupPoint.StoreId = scope.DefaultStoreId;
        await pickupPointService.InsertPickupPoint(pickupPoint);
        Success(translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Added"));
        return continueEditing
            ? RedirectToAction("EditPickupPoint", new { id = pickupPoint.Id })
            : RedirectToAction("PickupPoints");
    }

    protected virtual async Task<IActionResult> PickupPointEditGetCore(string id)
    {
        var pickupPoint = await pickupPointService.GetPickupPointById(id);
        if (pickupPoint == null || !await scope.HasAccess(pickupPoint)) return RedirectToAction("PickupPoints");
        var model = pickupPoint.ToModel();
        await PreparePickupPointModel(model);
        return View(model);
    }

    protected virtual async Task<IActionResult> PickupPointEditPostCore(PickupPointModel model, bool continueEditing)
    {
        var pickupPoint = await pickupPointService.GetPickupPointById(model.Id);
        if (pickupPoint == null || !await scope.HasAccess(pickupPoint)) return RedirectToAction("PickupPoints");
        if (!ModelState.IsValid)
        {
            await PreparePickupPointModel(model);
            return View(model);
        }
        pickupPoint = model.ToEntity(pickupPoint);
        if (scope.DefaultStoreId is not null) pickupPoint.StoreId = scope.DefaultStoreId;
        await pickupPointService.UpdatePickupPoint(pickupPoint);
        Success(translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Updated"));
        return continueEditing
            ? RedirectToAction("EditPickupPoint", new { id = pickupPoint.Id })
            : RedirectToAction("PickupPoints");
    }

    protected virtual async Task<IActionResult> PickupPointDeleteCore(string id)
    {
        var pickupPoint = await pickupPointService.GetPickupPointById(id);
        if (pickupPoint == null || !await scope.HasAccess(pickupPoint)) return RedirectToAction("PickupPoints");
        await pickupPointService.DeletePickupPoint(pickupPoint);
        Success(translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Deleted"));
        return RedirectToAction("PickupPoints");
    }
}
