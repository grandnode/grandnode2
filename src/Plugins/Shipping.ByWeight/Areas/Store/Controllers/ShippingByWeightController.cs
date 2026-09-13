using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shipping.ByWeight.Domain;
using Shipping.ByWeight.Models;
using Shipping.ByWeight.Services;
using System.Text;

namespace Shipping.ByWeight.Areas.Store.Controllers;

/// <summary>
///     Store-manager configuration of the "by weight" shipping rate provider.
///     A store owner can add, edit and delete rate records, but only the ones that belong
///     to his own store (<see cref="CurrentStoreId" />). Records owned by other stores or
///     global (store = *) records are never returned nor mutated, and the plugin settings
///     are saved as a per-store override rather than system wide.
/// </summary>
[Area("Store")]
[AuthorizeStore]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ShippingByWeightController : BaseController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly CurrencySettings _currencySettings;
    private readonly IMeasureService _measureService;
    private readonly MeasureSettings _measureSettings;
    private readonly ISettingService _settingService;
    private readonly IShippingByWeightService _shippingByWeightService;
    private readonly IShippingMethodService _shippingMethodService;
    private readonly ITranslationService _translationService;
    private readonly IWarehouseService _warehouseService;

    public ShippingByWeightController(
        IWarehouseService warehouseService,
        IShippingMethodService shippingMethodService,
        ICountryService countryService,
        IShippingByWeightService shippingByWeightService,
        ISettingService settingService,
        ITranslationService translationService,
        ICurrencyService currencyService,
        CurrencySettings currencySettings,
        IMeasureService measureService,
        MeasureSettings measureSettings,
        IContextAccessor contextAccessor)
    {
        _warehouseService = warehouseService;
        _shippingMethodService = shippingMethodService;
        _countryService = countryService;
        _shippingByWeightService = shippingByWeightService;
        _settingService = settingService;
        _translationService = translationService;
        _currencyService = currencyService;
        _currencySettings = currencySettings;
        _measureService = measureService;
        _measureSettings = measureSettings;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    ///     The store the current staff/store-manager is bound to.
    /// </summary>
    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public async Task<IActionResult> Configure()
    {
        //per-store override of the plugin settings - the global values are the fallback
        var settings = await _settingService.LoadSetting<ByWeightShippingSettings>(CurrentStoreId);

        var model = new ShippingByWeightListModel {
            LimitMethodsToCreated = settings.LimitMethodsToCreated,
            DisplayOrder = settings.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SaveGeneralSettings(ShippingByWeightListModel model)
    {
        var settings = await _settingService.LoadSetting<ByWeightShippingSettings>(CurrentStoreId);
        settings.LimitMethodsToCreated = model.LimitMethodsToCreated;
        settings.DisplayOrder = model.DisplayOrder;

        //save as a per-store override - never touch the system-wide value
        await _settingService.SaveSetting(settings, CurrentStoreId);

        return Json(new { Result = true });
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> RatesList(DataSourceRequest command)
    {
        //only the current store records - filtered and paged in the service layer
        var records = await _shippingByWeightService.GetAll(CurrentStoreId, command.Page - 1, command.PageSize);

        var sbwModel = new List<ShippingByWeightModel>();
        foreach (var x in records)
        {
            var m = new ShippingByWeightModel {
                Id = x.Id,
                StoreId = x.StoreId,
                WarehouseId = x.WarehouseId,
                ShippingMethodId = x.ShippingMethodId,
                CountryId = x.CountryId,
                From = x.From,
                To = x.To,
                AdditionalFixedCost = x.AdditionalFixedCost,
                PercentageRateOfSubtotal = x.PercentageRateOfSubtotal,
                RatePerWeightUnit = x.RatePerWeightUnit,
                LowerWeightLimit = x.LowerWeightLimit
            };
            //shipping method
            var shippingMethod = await _shippingMethodService.GetShippingMethodById(x.ShippingMethodId);
            m.ShippingMethodName = shippingMethod != null ? shippingMethod.Name : "Unavailable";
            //warehouse
            var warehouse = await _warehouseService.GetWarehouseById(x.WarehouseId);
            m.WarehouseName = warehouse != null ? warehouse.Name : "*";
            //country
            var c = await _countryService.GetCountryById(x.CountryId);
            m.CountryName = c != null ? c.Name : "*";
            //state
            var s = c?.StateProvinces.FirstOrDefault(y => y.Id == x.StateProvinceId);
            m.StateProvinceName = s != null ? s.Name : "*";
            //zip
            m.Zip = !string.IsNullOrEmpty(x.Zip) ? x.Zip : "*";

            var htmlSb = new StringBuilder("<div>");
            htmlSb.Append($"{_translationService.GetResource("Plugins.Shipping.ByWeight.Fields.From")}: {m.From}");
            htmlSb.Append("<br />");
            htmlSb.Append($"{_translationService.GetResource("Plugins.Shipping.ByWeight.Fields.To")}: {m.To}");
            htmlSb.Append("<br />");
            htmlSb.Append(
                $"{_translationService.GetResource("Plugins.Shipping.ByWeight.Fields.AdditionalFixedCost")}: {m.AdditionalFixedCost}");
            htmlSb.Append("<br />");
            htmlSb.Append(
                $"{_translationService.GetResource("Plugins.Shipping.ByWeight.Fields.RatePerWeightUnit")}: {m.RatePerWeightUnit}");
            htmlSb.Append("<br />");
            htmlSb.Append(
                $"{_translationService.GetResource("Plugins.Shipping.ByWeight.Fields.LowerWeightLimit")}: {m.LowerWeightLimit}");
            htmlSb.Append("<br />");
            htmlSb.Append(
                $"{_translationService.GetResource("Plugins.Shipping.ByWeight.Fields.PercentageRateOfSubtotal")}: {m.PercentageRateOfSubtotal}");
            htmlSb.Append("</div>");
            m.DataHtml = htmlSb.ToString();

            sbwModel.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = sbwModel,
            Total = records.TotalCount
        };

        return Json(gridModel);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> RateDelete(string id)
    {
        var sbw = await _shippingByWeightService.GetById(id);
        //guard: a store owner can only delete his own store records
        if (sbw == null || sbw.StoreId != CurrentStoreId)
            return new JsonResult("");

        await _shippingByWeightService.DeleteShippingByWeightRecord(sbw);

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> AddPopup()
    {
        var model = new ShippingByWeightModel {
            //the owner cannot create records for another store
            StoreId = CurrentStoreId,
            //CurrencySettings is resolved per current store, so this already honours a store override
            PrimaryStoreCurrencyCode =
                (await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode,
            BaseWeightIn = (await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)).Name,
            To = 1000000
        };

        var shippingMethods = await _shippingMethodService.GetAllShippingMethods(storeId: CurrentStoreId);
        if (shippingMethods.Count == 0)
            return Content("No shipping methods can be loaded");

        await PrepareSelectLists(model, shippingMethods, null, null, null);

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> AddPopup(ShippingByWeightModel model)
    {
        var sbw = new ShippingByWeightRecord {
            //force the current store - the owner cannot create records for another store
            StoreId = CurrentStoreId,
            WarehouseId = model.WarehouseId,
            CountryId = model.CountryId,
            StateProvinceId = model.StateProvinceId,
            Zip = model.Zip == "*" ? null : model.Zip,
            ShippingMethodId = model.ShippingMethodId,
            From = model.From,
            To = model.To,
            AdditionalFixedCost = model.AdditionalFixedCost,
            RatePerWeightUnit = model.RatePerWeightUnit,
            PercentageRateOfSubtotal = model.PercentageRateOfSubtotal,
            LowerWeightLimit = model.LowerWeightLimit
        };
        await _shippingByWeightService.InsertShippingByWeightRecord(sbw);

        ViewBag.RefreshPage = true;

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> EditPopup(string id)
    {
        var sbw = await _shippingByWeightService.GetById(id);
        //guard: a store owner can only open his own store records
        if (sbw == null || sbw.StoreId != CurrentStoreId)
            return RedirectToAction("Configure");

        var model = new ShippingByWeightModel {
            Id = sbw.Id,
            StoreId = sbw.StoreId,
            WarehouseId = sbw.WarehouseId,
            CountryId = sbw.CountryId,
            StateProvinceId = sbw.StateProvinceId,
            Zip = sbw.Zip,
            ShippingMethodId = sbw.ShippingMethodId,
            From = sbw.From,
            To = sbw.To,
            AdditionalFixedCost = sbw.AdditionalFixedCost,
            PercentageRateOfSubtotal = sbw.PercentageRateOfSubtotal,
            RatePerWeightUnit = sbw.RatePerWeightUnit,
            LowerWeightLimit = sbw.LowerWeightLimit,
            //CurrencySettings is resolved per current store, so this already honours a store override
            PrimaryStoreCurrencyCode =
                (await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode,
            BaseWeightIn = (await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)).Name
        };

        var shippingMethods = await _shippingMethodService.GetAllShippingMethods(storeId: CurrentStoreId);
        if (shippingMethods.Count == 0)
            return Content("No shipping methods can be loaded");

        await PrepareSelectLists(model, shippingMethods, sbw.WarehouseId, sbw.ShippingMethodId, sbw.CountryId,
            sbw.StateProvinceId);

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> EditPopup(ShippingByWeightModel model)
    {
        var sbw = await _shippingByWeightService.GetById(model.Id);
        //guard: a store owner can only edit his own store records
        if (sbw == null || sbw.StoreId != CurrentStoreId)
            return RedirectToAction("Configure");

        //StoreId is deliberately not taken from the model - the record stays in the owner's store
        sbw.WarehouseId = model.WarehouseId;
        sbw.CountryId = model.CountryId;
        sbw.StateProvinceId = model.StateProvinceId;
        sbw.Zip = model.Zip == "*" ? null : model.Zip;
        sbw.ShippingMethodId = model.ShippingMethodId;
        sbw.From = model.From;
        sbw.To = model.To;
        sbw.AdditionalFixedCost = model.AdditionalFixedCost;
        sbw.RatePerWeightUnit = model.RatePerWeightUnit;
        sbw.PercentageRateOfSubtotal = model.PercentageRateOfSubtotal;
        sbw.LowerWeightLimit = model.LowerWeightLimit;
        await _shippingByWeightService.UpdateShippingByWeightRecord(sbw);

        ViewBag.RefreshPage = true;

        return View(model);
    }

    /// <summary>
    ///     Fills the drop-downs of the add/edit form. No store selector is offered - the record
    ///     always belongs to the owner's own store - and warehouses are limited to the ones the
    ///     store may use (its own, plus the ones shared by every store).
    /// </summary>
    [NonAction]
    private async Task PrepareSelectLists(ShippingByWeightModel model,
        IList<Grand.Domain.Shipping.ShippingMethod> shippingMethods,
        string selectedWarehouseId, string selectedShippingMethodId, string selectedCountryId,
        string selectedStateProvinceId = null)
    {
        //warehouses
        model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "" });
        var warehouses = (await _warehouseService.GetAllWarehouses())
            .Where(x => string.IsNullOrEmpty(x.StoreId) || x.StoreId == CurrentStoreId);
        foreach (var warehouse in warehouses)
            model.AvailableWarehouses.Add(new SelectListItem {
                Text = warehouse.Name, Value = warehouse.Id,
                Selected = warehouse.Id == selectedWarehouseId
            });
        //shipping methods
        foreach (var sm in shippingMethods)
            model.AvailableShippingMethods.Add(new SelectListItem {
                Text = sm.Name, Value = sm.Id,
                Selected = sm.Id == selectedShippingMethodId
            });
        //countries
        model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "" });
        var countries = await _countryService.GetAllCountries(showHidden: true);
        foreach (var c in countries)
            model.AvailableCountries.Add(new SelectListItem {
                Text = c.Name, Value = c.Id,
                Selected = c.Id == selectedCountryId
            });
        //states
        model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "" });
        var selectedCountry = countries.FirstOrDefault(x => x.Id == selectedCountryId);
        if (selectedCountry == null) return;

        foreach (var s in await _countryService.GetStateProvincesByCountryId(selectedCountry.Id))
            model.AvailableStates.Add(new SelectListItem {
                Text = s.Name, Value = s.Id,
                Selected = s.Id == selectedStateProvinceId
            });
    }
}
