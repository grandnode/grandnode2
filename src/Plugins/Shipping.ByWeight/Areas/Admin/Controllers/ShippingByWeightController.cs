using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Directory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shipping.ByWeight.Domain;
using Shipping.ByWeight.Models;
using Shipping.ByWeight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shipping.ByWeight.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.ShippingSettings)]
    public class ShippingByWeightController : BaseShippingController
    {
        private readonly IWarehouseService _warehouseService;
        private readonly IShippingMethodService _shippingMethodService;
        private readonly IStoreService _storeService;
        private readonly ICountryService _countryService;
        private readonly ByWeightShippingSettings _shippingByWeightSettings;
        private readonly IShippingByWeightService _shippingByWeightService;
        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IMeasureService _measureService;
        private readonly MeasureSettings _measureSettings;

        public ShippingByWeightController(
            IWarehouseService warehouseService,
            IShippingMethodService shippingMethodService,
            IStoreService storeService,
            ICountryService countryService,
            ByWeightShippingSettings shippingByWeightSettings,
            IShippingByWeightService shippingByWeightService,
            ISettingService settingService,
            ITranslationService translationService,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            IMeasureService measureService,
            MeasureSettings measureSettings)
        {
            _warehouseService = warehouseService;
            _shippingMethodService = shippingMethodService;
            _storeService = storeService;
            _countryService = countryService;
            _shippingByWeightSettings = shippingByWeightSettings;
            _shippingByWeightService = shippingByWeightService;
            _settingService = settingService;
            _translationService = translationService;
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _measureService = measureService;
            _measureSettings = measureSettings;
        }
        public IActionResult Configure()
        {
            var model = new ShippingByWeightListModel();

            model.LimitMethodsToCreated = _shippingByWeightSettings.LimitMethodsToCreated;
            model.DisplayOrder = _shippingByWeightSettings.DisplayOrder;

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> SaveGeneralSettings(ShippingByWeightListModel model)
        {
            //save settings
            _shippingByWeightSettings.LimitMethodsToCreated = model.LimitMethodsToCreated;
            _shippingByWeightSettings.DisplayOrder = model.DisplayOrder;

            await _settingService.SaveSetting(_shippingByWeightSettings);

            return Json(new { Result = true });
        }


        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> RatesList(DataSourceRequest command)
        {
            var records = await _shippingByWeightService.GetAll(command.Page - 1, command.PageSize);

            var sbwModel = new List<ShippingByWeightModel>();
            foreach (var x in records)
            {

                var m = new ShippingByWeightModel
                {
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
                    LowerWeightLimit = x.LowerWeightLimit,
                };
                //shipping method
                var shippingMethod = await _shippingMethodService.GetShippingMethodById(x.ShippingMethodId);
                m.ShippingMethodName = (shippingMethod != null) ? shippingMethod.Name : "Unavailable";
                //store
                var store = await _storeService.GetStoreById(x.StoreId);
                m.StoreName = (store != null) ? store.Shortcut : "*";
                //warehouse
                var warehouse = await _warehouseService.GetWarehouseById(x.WarehouseId);
                m.WarehouseName = (warehouse != null) ? warehouse.Name : "*";
                //country
                var c = await _countryService.GetCountryById(x.CountryId);
                m.CountryName = (c != null) ? c.Name : "*";
                //state
                var s = c?.StateProvinces.FirstOrDefault(y => y.Id == x.StateProvinceId);
                m.StateProvinceName = (s != null) ? s.Name : "*";
                //zip
                m.Zip = (!String.IsNullOrEmpty(x.Zip)) ? x.Zip : "*";


                var htmlSb = new StringBuilder("<div>");
                htmlSb.AppendFormat("{0}: {1}", _translationService.GetResource("Plugins.Shipping.ByWeight.Fields.From"), m.From);
                htmlSb.Append("<br />");
                htmlSb.AppendFormat("{0}: {1}", _translationService.GetResource("Plugins.Shipping.ByWeight.Fields.To"), m.To);
                htmlSb.Append("<br />");
                htmlSb.AppendFormat("{0}: {1}", _translationService.GetResource("Plugins.Shipping.ByWeight.Fields.AdditionalFixedCost"), m.AdditionalFixedCost);
                htmlSb.Append("<br />");
                htmlSb.AppendFormat("{0}: {1}", _translationService.GetResource("Plugins.Shipping.ByWeight.Fields.RatePerWeightUnit"), m.RatePerWeightUnit);
                htmlSb.Append("<br />");
                htmlSb.AppendFormat("{0}: {1}", _translationService.GetResource("Plugins.Shipping.ByWeight.Fields.LowerWeightLimit"), m.LowerWeightLimit);
                htmlSb.Append("<br />");
                htmlSb.AppendFormat("{0}: {1}", _translationService.GetResource("Plugins.Shipping.ByWeight.Fields.PercentageRateOfSubtotal"), m.PercentageRateOfSubtotal);

                htmlSb.Append("</div>");
                m.DataHtml = htmlSb.ToString();

                sbwModel.Add(m);
            }
            var gridModel = new DataSourceResult
            {
                Data = sbwModel,
                Total = records.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> RateDelete(string id)
        {
            var sbw = await _shippingByWeightService.GetById(id);
            if (sbw != null)
                await _shippingByWeightService.DeleteShippingByWeightRecord(sbw);

            return new JsonResult("");
        }

        public async Task<IActionResult> AddPopup()
        {
            var model = new ShippingByWeightModel();
            model.PrimaryStoreCurrencyCode = (await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode;
            model.BaseWeightIn = (await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)).Name;
            model.To = 1000000;

            var shippingMethods = await _shippingMethodService.GetAllShippingMethods();
            if (shippingMethods.Count == 0)
                return Content("No shipping methods can be loaded");

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = " " });
            foreach (var store in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Shortcut, Value = store.Id.ToString() });
            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = " " });
            foreach (var warehouses in await _warehouseService.GetAllWarehouses())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouses.Name, Value = warehouses.Id.ToString() });
            //shipping methods
            foreach (var sm in shippingMethods)
                model.AvailableShippingMethods.Add(new SelectListItem { Text = sm.Name, Value = sm.Id.ToString() });
            //countries
            model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = " " });
            var countries = await _countryService.GetAllCountries(showHidden: true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //states
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "" });

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> AddPopup(ShippingByWeightModel model)
        {
            var sbw = new ShippingByWeightRecord
            {
                StoreId = model.StoreId,
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

        //edit
        public async Task<IActionResult> EditPopup(string id)
        {
            var sbw = await _shippingByWeightService.GetById(id);
            if (sbw == null)
                //No record found with the specified id
                return RedirectToAction("Configure");

            var model = new ShippingByWeightModel
            {
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
                PrimaryStoreCurrencyCode = (await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)).CurrencyCode,
                BaseWeightIn = (await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)).Name
            };

            var shippingMethods = await _shippingMethodService.GetAllShippingMethods();
            if (shippingMethods.Count == 0)
                return Content("No shipping methods can be loaded");

            var selectedStore = await _storeService.GetStoreById(sbw.StoreId);
            var selectedWarehouse = await _warehouseService.GetWarehouseById(sbw.WarehouseId);
            var selectedShippingMethod = await _shippingMethodService.GetShippingMethodById(sbw.ShippingMethodId);
            var selectedCountry = await _countryService.GetCountryById(sbw.CountryId);
            var selectedState = selectedCountry?.StateProvinces.FirstOrDefault(x => x.Id == sbw.StateProvinceId);
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "" });
            foreach (var store in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Shortcut, Value = store.Id.ToString(), Selected = (selectedStore != null && store.Id == selectedStore.Id) });
            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "" });
            foreach (var warehouse in await _warehouseService.GetAllWarehouses())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id.ToString(), Selected = (selectedWarehouse != null && warehouse.Id == selectedWarehouse.Id) });
            //shipping methods
            foreach (var sm in shippingMethods)
                model.AvailableShippingMethods.Add(new SelectListItem { Text = sm.Name, Value = sm.Id.ToString(), Selected = (selectedShippingMethod != null && sm.Id == selectedShippingMethod.Id) });
            //countries
            model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "" });
            var countries = await _countryService.GetAllCountries(showHidden: true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (selectedCountry != null && c.Id == selectedCountry.Id) });
            //states
            var states = selectedCountry != null ? await _countryService.GetStateProvincesByCountryId(selectedCountry.Id, showHidden: true) : new List<StateProvince>();
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "" });
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (selectedState != null && s.Id == selectedState.Id) });

            return View(model);
        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> EditPopup(ShippingByWeightModel model)
        {
            var sbw = await _shippingByWeightService.GetById(model.Id);
            if (sbw == null)
                //No record found with the specified id
                return RedirectToAction("Configure");

            sbw.StoreId = model.StoreId;
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
    }
}
