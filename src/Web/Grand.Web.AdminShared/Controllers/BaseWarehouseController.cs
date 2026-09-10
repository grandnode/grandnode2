using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Directory;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

[SharedViewFolder("Shipping")]
public abstract class BaseWarehouseController(
    IWarehouseService warehouseService,
    ICountryService countryService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<Grand.Domain.Shipping.Warehouse> scope) : BaseController
{
    protected virtual async Task PrepareWarehouseModel(WarehouseModel model)
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
                Text = translationService.GetResource("Admin.Configuration.Shipping.Warehouses.SelectStore"),
                Value = ""
            });
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id, Selected = s.Id == model.StoreId });
        }
    }

    protected virtual async Task<IActionResult> WarehouseListCore()
    {
        var storeMap = (await storeService.GetAllStores()).ToDictionary(s => s.Id, s => s.Shortcut);
        var warehouses = (await warehouseService.GetAllWarehouses(scope.DefaultStoreId ?? "")).ToList();
        var model = warehouses.Select(x => {
            var m = x.ToModel();
            m.StoreName = !string.IsNullOrEmpty(x.StoreId) && storeMap.TryGetValue(x.StoreId, out var name) ? name : "";
            return m;
        }).ToList();
        return Json(new DataSourceResult { Data = model, Total = model.Count });
    }

    protected virtual async Task<IActionResult> WarehouseCreateGetCore()
    {
        var model = new WarehouseModel();
        await PrepareWarehouseModel(model);
        return View(model);
    }

    protected virtual async Task<IActionResult> WarehouseCreatePostCore(WarehouseModel model, bool continueEditing)
    {
        if (!ModelState.IsValid)
        {
            await PrepareWarehouseModel(model);
            return View(model);
        }
        var warehouse = model.ToEntity();
        warehouse.Address = model.Address.ToEntity();
        if (scope.DefaultStoreId is not null) warehouse.StoreId = scope.DefaultStoreId;
        await warehouseService.InsertWarehouse(warehouse);
        Success(translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Added"));
        return continueEditing
            ? RedirectToAction("EditWarehouse", new { id = warehouse.Id })
            : RedirectToAction("Warehouses");
    }

    protected virtual async Task<IActionResult> WarehouseEditGetCore(string id)
    {
        var warehouse = await warehouseService.GetWarehouseById(id);
        if (warehouse == null || !await scope.HasAccess(warehouse)) return RedirectToAction("Warehouses");
        var model = warehouse.ToModel();
        await PrepareWarehouseModel(model);
        return View(model);
    }

    protected virtual async Task<IActionResult> WarehouseEditPostCore(WarehouseModel model, bool continueEditing)
    {
        var warehouse = await warehouseService.GetWarehouseById(model.Id);
        if (warehouse == null || !await scope.HasAccess(warehouse)) return RedirectToAction("Warehouses");
        if (!ModelState.IsValid)
        {
            await PrepareWarehouseModel(model);
            return View(model);
        }
        warehouse = model.ToEntity(warehouse);
        warehouse.Address = model.Address.ToEntity();
        if (scope.DefaultStoreId is not null) warehouse.StoreId = scope.DefaultStoreId;
        await warehouseService.UpdateWarehouse(warehouse);
        Success(translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Updated"));
        return continueEditing
            ? RedirectToAction("EditWarehouse", new { id = warehouse.Id })
            : RedirectToAction("Warehouses");
    }

    protected virtual async Task<IActionResult> WarehouseDeleteCore(string id)
    {
        var warehouse = await warehouseService.GetWarehouseById(id);
        if (warehouse == null || !await scope.HasAccess(warehouse)) return RedirectToAction("Warehouses");
        await warehouseService.DeleteWarehouse(warehouse);
        Success(translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Deleted"));
        return RedirectToAction("Warehouses");
    }
}
