using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ShippingController(
    IDeliveryDateService deliveryDateService,
    IWarehouseService warehouseService,
    IPickupPointService pickupPointService,
    ICountryService countryService,
    ILanguageService languageService,
    ITranslationService translationService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    #region Utilities

    private async Task PrepareAddressModel(AddressModel model, string selectedCountryId)
    {
        model.AvailableCountries.Add(new SelectListItem { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id, Selected = c.Id == selectedCountryId });

        var states = !string.IsNullOrEmpty(selectedCountryId)
            ? (await countryService.GetCountryById(selectedCountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states?.Count > 0)
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id, Selected = s.Id == model.StateProvinceId });

        model.CountryEnabled = true;
        model.StateProvinceEnabled = true;
        model.CityEnabled = true;
        model.StreetAddressEnabled = true;
        model.ZipPostalCodeEnabled = true;
        model.ZipPostalCodeRequired = true;
        model.PhoneEnabled = true;
        model.FaxEnabled = true;
        model.CompanyEnabled = true;
    }

    private async Task PrepareWarehouseModel(WarehouseModel model)
    {
        await PrepareAddressModel(model.Address, model.Address.CountryId);
    }

    private async Task PreparePickupPointModel(PickupPointModel model)
    {
        await PrepareAddressModel(model.Address, model.Address.CountryId);

        model.AvailableWarehouses.Add(new SelectListItem { Text = translationService.GetResource("Admin.Configuration.Shipping.PickupPoint.SelectWarehouse"), Value = "" });
        foreach (var w in await warehouseService.GetAllWarehouses(CurrentStoreId))
            model.AvailableWarehouses.Add(new SelectListItem { Text = w.Name, Value = w.Id, Selected = w.Id == model.WarehouseId });
    }

    #endregion

    #region Delivery dates

    public IActionResult DeliveryDates()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> DeliveryDatesListData()
    {
        var storeId = CurrentStoreId;
        var deliveryDates = (await deliveryDateService.GetAllDeliveryDates(storeId))
            .Where(d => d.StoreId == storeId)
            .ToList();
        var gridModel = new DataSourceResult {
            Data = deliveryDates.Select(d => d.ToModel()),
            Total = deliveryDates.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> CreateDeliveryDate()
    {
        var model = new DeliveryDateModel { ColorSquaresRgb = "#000000" };
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> CreateDeliveryDate(DeliveryDateModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var deliveryDate = model.ToEntity();
            deliveryDate.StoreId = CurrentStoreId;
            await deliveryDateService.InsertDeliveryDate(deliveryDate);
            Success(translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Added"));
            return continueEditing
                ? RedirectToAction("EditDeliveryDate", new { id = deliveryDate.Id })
                : RedirectToAction("DeliveryDates");
        }
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> EditDeliveryDate(string id)
    {
        var deliveryDate = await deliveryDateService.GetDeliveryDateById(id);
        if (deliveryDate == null || deliveryDate.StoreId != CurrentStoreId)
            return RedirectToAction("DeliveryDates");

        var model = deliveryDate.ToModel();
        if (string.IsNullOrEmpty(model.ColorSquaresRgb)) model.ColorSquaresRgb = "#000000";
        await AddLocales(languageService, model.Locales, (locale, languageId) => {
            locale.Name = deliveryDate.GetTranslation(x => x.Name, languageId, false);
        });
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> EditDeliveryDate(DeliveryDateModel model, bool continueEditing)
    {
        var deliveryDate = await deliveryDateService.GetDeliveryDateById(model.Id);
        if (deliveryDate == null || deliveryDate.StoreId != CurrentStoreId)
            return RedirectToAction("DeliveryDates");

        if (ModelState.IsValid)
        {
            deliveryDate = model.ToEntity(deliveryDate);
            deliveryDate.StoreId = CurrentStoreId;
            await deliveryDateService.UpdateDeliveryDate(deliveryDate);
            Success(translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Updated"));
            return continueEditing
                ? RedirectToAction("EditDeliveryDate", new { id = deliveryDate.Id })
                : RedirectToAction("DeliveryDates");
        }
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> DeleteDeliveryDate(string id)
    {
        var deliveryDate = await deliveryDateService.GetDeliveryDateById(id);
        if (deliveryDate == null || deliveryDate.StoreId != CurrentStoreId)
            return RedirectToAction("DeliveryDates");

        await deliveryDateService.DeleteDeliveryDate(deliveryDate);
        Success(translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Deleted"));
        return RedirectToAction("DeliveryDates");
    }

    #endregion

    #region Warehouses

    public IActionResult Warehouses()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> WarehousesListData()
    {
        var storeId = CurrentStoreId;
        var warehouses = (await warehouseService.GetAllWarehouses(storeId))
            .Where(w => w.StoreId == storeId)
            .ToList();
        var gridModel = new DataSourceResult {
            Data = warehouses.Select(w => w.ToModel()),
            Total = warehouses.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> CreateWarehouse()
    {
        var model = new WarehouseModel();
        await PrepareWarehouseModel(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> CreateWarehouse(WarehouseModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var warehouse = model.ToEntity();
            warehouse.Address = model.Address.ToEntity();
            warehouse.StoreId = CurrentStoreId;
            await warehouseService.InsertWarehouse(warehouse);
            Success(translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Added"));
            return continueEditing
                ? RedirectToAction("EditWarehouse", new { id = warehouse.Id })
                : RedirectToAction("Warehouses");
        }
        await PrepareWarehouseModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> EditWarehouse(string id)
    {
        var warehouse = await warehouseService.GetWarehouseById(id);
        if (warehouse == null || warehouse.StoreId != CurrentStoreId)
            return RedirectToAction("Warehouses");

        var model = warehouse.ToModel();
        await PrepareWarehouseModel(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> EditWarehouse(WarehouseModel model, bool continueEditing)
    {
        var warehouse = await warehouseService.GetWarehouseById(model.Id);
        if (warehouse == null || warehouse.StoreId != CurrentStoreId)
            return RedirectToAction("Warehouses");

        if (ModelState.IsValid)
        {
            warehouse = model.ToEntity(warehouse);
            warehouse.Address = model.Address.ToEntity();
            warehouse.StoreId = CurrentStoreId;
            await warehouseService.UpdateWarehouse(warehouse);
            Success(translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Updated"));
            return continueEditing
                ? RedirectToAction("EditWarehouse", new { id = warehouse.Id })
                : RedirectToAction("Warehouses");
        }
        await PrepareWarehouseModel(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> DeleteWarehouse(string id)
    {
        var warehouse = await warehouseService.GetWarehouseById(id);
        if (warehouse == null || warehouse.StoreId != CurrentStoreId)
            return RedirectToAction("Warehouses");

        await warehouseService.DeleteWarehouse(warehouse);
        Success(translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Deleted"));
        return RedirectToAction("Warehouses");
    }

    #endregion

    #region Pickup points

    public IActionResult PickupPoints()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> PickupPointsListData()
    {
        var storeId = CurrentStoreId;
        var pickupPoints = (await pickupPointService.GetAllPickupPoints(storeId))
            .Where(p => p.StoreId == storeId)
            .ToList();
        var gridModel = new DataSourceResult {
            Data = pickupPoints.Select(p => p.ToModel()),
            Total = pickupPoints.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> CreatePickupPoint()
    {
        var model = new PickupPointModel();
        await PreparePickupPointModel(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> CreatePickupPoint(PickupPointModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var pickupPoint = model.ToEntity();
            pickupPoint.StoreId = CurrentStoreId;
            await pickupPointService.InsertPickupPoint(pickupPoint);
            Success(translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Added"));
            return continueEditing
                ? RedirectToAction("EditPickupPoint", new { id = pickupPoint.Id })
                : RedirectToAction("PickupPoints");
        }
        await PreparePickupPointModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> EditPickupPoint(string id)
    {
        var pickupPoint = await pickupPointService.GetPickupPointById(id);
        if (pickupPoint == null || pickupPoint.StoreId != CurrentStoreId)
            return RedirectToAction("PickupPoints");

        var model = pickupPoint.ToModel();
        await PreparePickupPointModel(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [Grand.Web.Common.Filters.ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> EditPickupPoint(PickupPointModel model, bool continueEditing)
    {
        var pickupPoint = await pickupPointService.GetPickupPointById(model.Id);
        if (pickupPoint == null || pickupPoint.StoreId != CurrentStoreId)
            return RedirectToAction("PickupPoints");

        if (ModelState.IsValid)
        {
            pickupPoint = model.ToEntity(pickupPoint);
            pickupPoint.StoreId = CurrentStoreId;
            await pickupPointService.UpdatePickupPoint(pickupPoint);
            Success(translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Updated"));
            return continueEditing
                ? RedirectToAction("EditPickupPoint", new { id = pickupPoint.Id })
                : RedirectToAction("PickupPoints");
        }
        await PreparePickupPointModel(model);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> DeletePickupPoint(string id)
    {
        var pickupPoint = await pickupPointService.GetPickupPointById(id);
        if (pickupPoint == null || pickupPoint.StoreId != CurrentStoreId)
            return RedirectToAction("PickupPoints");

        await pickupPointService.DeletePickupPoint(pickupPoint);
        Success(translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Deleted"));
        return RedirectToAction("PickupPoints");
    }

    #endregion
}
