using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Directory;
using Grand.Web.Admin.Models.Shipping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Web.Admin.Models.Common;
using Grand.Business.Common.Interfaces.Logging;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.ShippingSettings)]
    public partial class ShippingController : BaseAdminController
    {
        #region Fields

        private readonly IShippingService _shippingService;
        private readonly IShippingMethodService _shippingMethodService;
        private readonly IPickupPointService _pickupPointService;
        private readonly IDeliveryDateService _deliveryDateService;
        private readonly IWarehouseService _warehouseService;
        private readonly ISettingService _settingService;
        private readonly ICountryService _countryService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IStoreService _storeService;
        private readonly IGroupService _groupService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkContext _workContext;
        #endregion

        #region Constructors

        public ShippingController(
            IShippingService shippingService,
            IShippingMethodService shippingMethodService,
            IPickupPointService pickupPointService,
            IDeliveryDateService deliveryDateService,
            IWarehouseService warehouseService,
            ISettingService settingService,
            ICountryService countryService,
            ITranslationService translationService,
            ILanguageService languageService,
            IStoreService storeService,
            IGroupService groupService,
            IServiceProvider serviceProvider,
            IWorkContext workContext)
        {
            _shippingService = shippingService;
            _shippingMethodService = shippingMethodService;
            _pickupPointService = pickupPointService;
            _deliveryDateService = deliveryDateService;
            _warehouseService = warehouseService;
            _settingService = settingService;
            _countryService = countryService;
            _translationService = translationService;
            _languageService = languageService;
            _storeService = storeService;
            _groupService = groupService;
            _serviceProvider = serviceProvider;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        protected virtual async Task PrepareAddressWarehouseModel(WarehouseModel model)
        {
            model.Address.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.Address.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (c.Id == model.Address.CountryId) });
            //states
            var states = !String.IsNullOrEmpty(model.Address.CountryId) ? (await _countryService.GetCountryById(model.Address.CountryId))?.StateProvinces : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.Address.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == model.Address.StateProvinceId) });
            }

            model.Address.CountryEnabled = true;
            model.Address.StateProvinceEnabled = true;
            model.Address.CityEnabled = true;
            model.Address.StreetAddressEnabled = true;
            model.Address.ZipPostalCodeEnabled = true;
            model.Address.ZipPostalCodeRequired = true;
            model.Address.PhoneEnabled = true;
            model.Address.FaxEnabled = true;
            model.Address.CompanyEnabled = true;
        }

        protected virtual async Task PreparePickupPointModel(PickupPointModel model)
        {
            model.Address.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.Address.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (c.Id == model.Address.CountryId) });
            //states
            var states = !String.IsNullOrEmpty(model.Address.CountryId) ? (await _countryService.GetCountryById(model.Address.CountryId))?.StateProvinces : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.Address.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == model.Address.StateProvinceId) });
            }

            model.Address.CountryEnabled = true;
            model.Address.StateProvinceEnabled = true;
            model.Address.CityEnabled = true;
            model.Address.StreetAddressEnabled = true;
            model.Address.ZipPostalCodeEnabled = true;
            model.Address.ZipPostalCodeRequired = true;
            model.Address.PhoneEnabled = true;
            model.Address.FaxEnabled = true;
            model.Address.CompanyEnabled = true;

            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Configuration.Shipping.PickupPoint.SelectStore"), Value = "" });
            foreach (var c in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = c.Shortcut, Value = c.Id.ToString() });

            model.AvailableWarehouses.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Configuration.Shipping.PickupPoint.SelectWarehouse"), Value = "" });
            foreach (var c in await _warehouseService.GetAllWarehouses())
                model.AvailableWarehouses.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

        }

        #endregion

        #region Shipping rate  methods

        public IActionResult Providers() => View();

        [HttpPost]
        public IActionResult Providers(DataSourceRequest command)
        {
            var _shippingProviderSettings = _settingService.LoadSetting<ShippingProviderSettings>();
            var shippingProvidersModel = new List<ShippingRateComputationMethodModel>();
            var shippingProviders = _shippingService.LoadAllShippingRateCalculationProviders();
            foreach (var shippingProvider in shippingProviders)
            {
                var tmp1 = shippingProvider.ToModel();
                tmp1.IsActive = shippingProvider.IsShippingRateMethodActive(_shippingProviderSettings);
                var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == shippingProvider.SystemName);
                if (pluginInfo != null)
                {
                    var plugin = pluginInfo.Instance<IPlugin>(_serviceProvider);
                    if (plugin != null)
                    {
                        tmp1.ConfigurationUrl = plugin.ConfigurationUrl();
                        tmp1.LogoUrl = pluginInfo.GetLogoUrl(_workContext);
                    }
                }
                shippingProvidersModel.Add(tmp1);
            }
            shippingProvidersModel = shippingProvidersModel.ToList();
            var gridModel = new DataSourceResult
            {
                Data = shippingProvidersModel,
                Total = shippingProvidersModel.Count()
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProviderUpdate(ShippingRateComputationMethodModel model)
        {
            var _shippingProviderSettings = _settingService.LoadSetting<ShippingProviderSettings>();

            var srcm = _shippingService.LoadShippingRateCalculationProviderBySystemName(model.SystemName);
            if (srcm.IsShippingRateMethodActive(_shippingProviderSettings))
            {
                if (!model.IsActive)
                {
                    //mark as disabled
                    _shippingProviderSettings.ActiveSystemNames.Remove(srcm.SystemName);
                    await _settingService.SaveSetting(_shippingProviderSettings);
                }
            }
            else
            {
                if (model.IsActive)
                {
                    //mark as active
                    _shippingProviderSettings.ActiveSystemNames.Add(srcm.SystemName);
                    await _settingService.SaveSetting(_shippingProviderSettings);
                }
            }
            return new JsonResult("");
        }

        public IActionResult ConfigureProvider(string systemName)
        {
            var _shippingProviderSettings = _settingService.LoadSetting<ShippingProviderSettings>();

            var srcm = _shippingService.LoadShippingRateCalculationProviderBySystemName(systemName);
            if (srcm == null)
                //No Shipping rate  method found with the specified id
                return RedirectToAction("Providers");

            var model = srcm.ToModel();
            model.IsActive = srcm.IsShippingRateMethodActive(_shippingProviderSettings);

            var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == srcm.SystemName);
            if (pluginInfo != null)
            {
                var plugin = pluginInfo.Instance<IPlugin>(_serviceProvider);
                if (plugin != null)
                {
                    model.ConfigurationUrl = plugin.ConfigurationUrl();
                    model.LogoUrl = pluginInfo.GetLogoUrl(_workContext);
                }
            }

            return View(model);
        }

        #endregion

        #region Shipping methods

        public IActionResult Methods() => View();

        [HttpPost]
        public async Task<IActionResult> Methods(DataSourceRequest command)
        {
            var shippingMethodsModel = (await _shippingMethodService.GetAllShippingMethods())
                .Select(x => x.ToModel())
                .ToList();
            var gridModel = new DataSourceResult
            {
                Data = shippingMethodsModel,
                Total = shippingMethodsModel.Count
            };

            return Json(gridModel);
        }


        public async Task<IActionResult> CreateMethod()
        {
            var model = new ShippingMethodModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> CreateMethod(ShippingMethodModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var sm = model.ToEntity();
                await _shippingMethodService.InsertShippingMethod(sm);

                Success(_translationService.GetResource("Admin.Configuration.Shipping.Methods.Added"));
                return continueEditing ? RedirectToAction("EditMethod", new { id = sm.Id }) : RedirectToAction("Methods");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<IActionResult> EditMethod(string id)
        {
            var sm = await _shippingMethodService.GetShippingMethodById(id);
            if (sm == null)
                //No shipping method found with the specified id
                return RedirectToAction("Methods");

            var model = sm.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = sm.GetTranslation(x => x.Name, languageId, false);
                locale.Description = sm.GetTranslation(x => x.Description, languageId, false);
            });

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> EditMethod(ShippingMethodModel model, bool continueEditing)
        {
            var sm = await _shippingMethodService.GetShippingMethodById(model.Id);
            if (sm == null)
                //No shipping method found with the specified id
                return RedirectToAction("Methods");

            if (ModelState.IsValid)
            {
                sm = model.ToEntity(sm);
                await _shippingMethodService.UpdateShippingMethod(sm);

                Success(_translationService.GetResource("Admin.Configuration.Shipping.Methods.Updated"));
                return continueEditing ? RedirectToAction("EditMethod", new { id = sm.Id }) : RedirectToAction("Methods");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMethod(string id)
        {
            var sm = await _shippingMethodService.GetShippingMethodById(id);
            if (sm == null)
                //No shipping method found with the specified id
                return RedirectToAction("Methods");

            await _shippingMethodService.DeleteShippingMethod(sm);

            Success(_translationService.GetResource("Admin.Configuration.Shipping.Methods.Deleted"));
            return RedirectToAction("Methods");
        }

        #endregion

        #region Shipping Settings

        public async Task<IActionResult> Settings()
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var shippingSettings = _settingService.LoadSetting<ShippingSettings>(storeScope);
            var model = shippingSettings.ToModel();
            model.ActiveStore = storeScope;

            //shipping origin
            var originAddress = shippingSettings.ShippingOriginAddress;
            if (originAddress != null)
            {
                model.ShippingOriginAddress = await originAddress.ToModel(_countryService);
            }
            else
                model.ShippingOriginAddress = new AddressModel();

            model.ShippingOriginAddress.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.ShippingOriginAddress.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (originAddress != null && c.Id == originAddress.CountryId) });

            var states = originAddress != null && !String.IsNullOrEmpty(originAddress.CountryId) ? (await _countryService.GetCountryById(originAddress.CountryId))?.StateProvinces : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.ShippingOriginAddress.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == originAddress.StateProvinceId) });
            }

            model.ShippingOriginAddress.CountryEnabled = true;
            model.ShippingOriginAddress.StateProvinceEnabled = true;
            model.ShippingOriginAddress.CityEnabled = true;
            model.ShippingOriginAddress.StreetAddressEnabled = true;
            model.ShippingOriginAddress.ZipPostalCodeEnabled = true;
            model.ShippingOriginAddress.ZipPostalCodeRequired = true;

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Settings(ShippingSettingsModel model, 
            [FromServices] ICustomerActivityService customerActivityService)
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var shippingSettings = _settingService.LoadSetting<ShippingSettings>(storeScope);
            shippingSettings = model.ToEntity(shippingSettings);

            await _settingService.SaveSetting(shippingSettings, storeScope);

            //activity log
            await customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return RedirectToAction("Settings");
        }

        #endregion

        #region Delivery dates

        public IActionResult DeliveryDates() => View();

        [HttpPost]
        public async Task<IActionResult> DeliveryDates(DataSourceRequest command)
        {
            var deliveryDatesModel = (await _deliveryDateService.GetAllDeliveryDates())
                .Select(x => x.ToModel())
                .ToList();
            var gridModel = new DataSourceResult
            {
                Data = deliveryDatesModel,
                Total = deliveryDatesModel.Count
            };

            return Json(gridModel);
        }

        public async Task<IActionResult> CreateDeliveryDate()
        {
            var model = new DeliveryDateModel
            {
                ColorSquaresRgb = "#000000"
            };
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> CreateDeliveryDate(DeliveryDateModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var deliveryDate = model.ToEntity();
                await _deliveryDateService.InsertDeliveryDate(deliveryDate);
                Success(_translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Added"));
                return continueEditing ? RedirectToAction("EditDeliveryDate", new { id = deliveryDate.Id }) : RedirectToAction("DeliveryDates");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<IActionResult> EditDeliveryDate(string id)
        {
            var deliveryDate = await _deliveryDateService.GetDeliveryDateById(id);
            if (deliveryDate == null)
                //No delivery date found with the specified id
                return RedirectToAction("DeliveryDates");

            var model = deliveryDate.ToModel();

            if (String.IsNullOrEmpty(model.ColorSquaresRgb))
            {
                model.ColorSquaresRgb = "#000000";
            }

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = deliveryDate.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> EditDeliveryDate(DeliveryDateModel model, bool continueEditing)
        {
            var deliveryDate = await _deliveryDateService.GetDeliveryDateById(model.Id);
            if (deliveryDate == null)
                //No delivery date found with the specified id
                return RedirectToAction("DeliveryDates");

            if (ModelState.IsValid)
            {
                deliveryDate = model.ToEntity(deliveryDate);
                await _deliveryDateService.UpdateDeliveryDate(deliveryDate);
                //locales
                Success(_translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Updated"));
                return continueEditing ? RedirectToAction("EditDeliveryDate", new { id = deliveryDate.Id }) : RedirectToAction("DeliveryDates");
            }


            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDeliveryDate(string id)
        {
            var deliveryDate = await _deliveryDateService.GetDeliveryDateById(id);
            if (deliveryDate == null)
                //No delivery date found with the specified id
                return RedirectToAction("DeliveryDates");
            if (ModelState.IsValid)
            {
                await _deliveryDateService.DeleteDeliveryDate(deliveryDate);

                Success(_translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Deleted"));
                return RedirectToAction("DeliveryDates");
            }
            Error(ModelState);
            return RedirectToAction("EditDeliveryDate", new { id = id });
        }

        #endregion

        #region Warehouses

        public IActionResult Warehouses() => View();

        [HttpPost]
        public async Task<IActionResult> Warehouses(DataSourceRequest command)
        {
            var warehousesModel = (await _warehouseService.GetAllWarehouses())
                .Select(x => x.ToModel())
                .ToList();
            var gridModel = new DataSourceResult
            {
                Data = warehousesModel,
                Total = warehousesModel.Count
            };

            return Json(gridModel);
        }
        public async Task<IActionResult> CreateWarehouse()
        {
            var model = new WarehouseModel();
            await PrepareAddressWarehouseModel(model);
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> CreateWarehouse(WarehouseModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var warehouse = model.ToEntity();
                var address = model.Address.ToEntity();
                address.CreatedOnUtc = DateTime.UtcNow;
                warehouse.Address = address;
                await _warehouseService.InsertWarehouse(warehouse);

                Success(_translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Added"));
                return continueEditing ? RedirectToAction("EditWarehouse", new { id = warehouse.Id }) : RedirectToAction("Warehouses");
            }

            //If we got this far, something failed, redisplay form
            await PrepareAddressWarehouseModel(model);
            return View(model);
        }

        public async Task<IActionResult> EditWarehouse(string id)
        {
            var warehouse = await _warehouseService.GetWarehouseById(id);
            if (warehouse == null)
                //No warehouse found with the specified id
                return RedirectToAction("Warehouses");

            var model = warehouse.ToModel();
            await PrepareAddressWarehouseModel(model);
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> EditWarehouse(WarehouseModel model, bool continueEditing)
        {
            var warehouse = await _warehouseService.GetWarehouseById(model.Id);
            if (warehouse == null)
                //No warehouse found with the specified id
                return RedirectToAction("Warehouses");

            if (ModelState.IsValid)
            {
                warehouse = model.ToEntity(warehouse);
                await _warehouseService.UpdateWarehouse(warehouse);
                Success(_translationService.GetResource("Admin.Configuration.Shipping.Warehouses.Updated"));
                return continueEditing ? RedirectToAction("EditWarehouse", new { id = warehouse.Id }) : RedirectToAction("Warehouses");
            }

            //If we got this far, something failed, redisplay form
            await PrepareAddressWarehouseModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteWarehouse(string id)
        {
            var warehouse = await _warehouseService.GetWarehouseById(id);
            if (warehouse == null)
                //No warehouse found with the specified id
                return RedirectToAction("Warehouses");

            await _warehouseService.DeleteWarehouse(warehouse);

            Success(_translationService.GetResource("Admin.Configuration.Shipping.warehouses.Deleted"));
            return RedirectToAction("Warehouses");
        }

        #endregion

        #region PickupPoints

        public IActionResult PickupPoints() => View();

        [HttpPost]
        public async Task<IActionResult> PickupPoints(DataSourceRequest command)
        {
            var pickupPointsModel = (await _pickupPointService.GetAllPickupPoints())
                .Select(x => x.ToModel())
                .ToList();

            var gridModel = new DataSourceResult
            {
                Data = pickupPointsModel,
                Total = pickupPointsModel.Count
            };

            return Json(gridModel);
        }

        public async Task<IActionResult> CreatePickupPoint()
        {
            var model = new PickupPointModel();
            await PreparePickupPointModel(model);
            return View(model);
        }


        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> CreatePickupPoint(PickupPointModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var pickuppoint = model.ToEntity();
                await _pickupPointService.InsertPickupPoint(pickuppoint);

                Success(_translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Added"));
                return continueEditing ? RedirectToAction("EditPickupPoint", new { id = pickuppoint.Id }) : RedirectToAction("PickupPoints");
            }

            //If we got this far, something failed, redisplay form
            await PreparePickupPointModel(model);
            return View(model);
        }

        public async Task<IActionResult> EditPickupPoint(string id)
        {
            var pickuppoint = await _pickupPointService.GetPickupPointById(id);
            if (pickuppoint == null)
                //No pickup pint found with the specified id
                return RedirectToAction("PickupPoints");

            var model = pickuppoint.ToModel();
            await PreparePickupPointModel(model);

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> EditPickupPoint(PickupPointModel model, bool continueEditing)
        {
            var pickupPoint = await _pickupPointService.GetPickupPointById(model.Id);
            if (pickupPoint == null)
                //No pickup point found with the specified id
                return RedirectToAction("PickupPoints");

            if (ModelState.IsValid)
            {
                pickupPoint = model.ToEntity(pickupPoint);
                await _pickupPointService.UpdatePickupPoint(pickupPoint);

                Success(_translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Updated"));
                return continueEditing ? RedirectToAction("EditPickupPoint", new { id = pickupPoint.Id }) : RedirectToAction("PickupPoints");
            }
            //If we got this far, something failed, redisplay form
            await PreparePickupPointModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePickupPoint(string id)
        {
            var pickupPoint = await _pickupPointService.GetPickupPointById(id);
            if (pickupPoint == null)
                //No pickup point found with the specified id
                return RedirectToAction("PickupPoints");

            await _pickupPointService.DeletePickupPoint(pickupPoint);

            Success(_translationService.GetResource("Admin.Configuration.Shipping.PickupPoints.Deleted"));
            return RedirectToAction("PickupPoints");
        }

        #endregion

        #region Restrictions

        public async Task<IActionResult> Restrictions()
        {
            var model = new ShippingMethodRestrictionModel();

            var countries = await _countryService.GetAllCountries(showHidden: true);
            var shippingMethods = await _shippingMethodService.GetAllShippingMethods();
            var customerGroups = await _groupService.GetAllCustomerGroups();

            foreach (var country in countries)
            {
                model.AvailableCountries.Add(new CountryModel
                {
                    Id = country.Id,
                    Name = country.Name
                });
            }
            foreach (var sm in shippingMethods)
            {
                model.AvailableShippingMethods.Add(new ShippingMethodModel
                {
                    Id = sm.Id,
                    Name = sm.Name
                });
            }
            foreach (var r in customerGroups)
            {
                model.AvailableCustomerGroups.Add(new CustomerGroupModel() { Id = r.Id, Name = r.Name });
            }

            foreach (var country in countries)
            {
                foreach (var shippingMethod in shippingMethods)
                {
                    bool restricted = shippingMethod.CountryRestrictionExists(country.Id);
                    if (!model.Restricted.ContainsKey(country.Id))
                        model.Restricted[country.Id] = new Dictionary<string, bool>();
                    model.Restricted[country.Id][shippingMethod.Id] = restricted;
                }
            }

            foreach (var role in customerGroups)
            {
                foreach (var shippingMethod in shippingMethods)
                {
                    bool restricted = shippingMethod.CustomerGroupRestrictionExists(role.Id);
                    if (!model.RestictedGroup.ContainsKey(role.Id))
                        model.RestictedGroup[role.Id] = new Dictionary<string, bool>();
                    model.RestictedGroup[role.Id][shippingMethod.Id] = restricted;
                }
            }


            return View(model);
        }

        [HttpPost, ActionName("Restrictions")]
        [RequestFormLimits(ValueCountLimit = 2048)]
        public async Task<IActionResult> RestrictionSave(IFormCollection form)
        {
            var countries = await _countryService.GetAllCountries(showHidden: true);
            var shippingMethods = await _shippingMethodService.GetAllShippingMethods();
            var customerGroups = await _groupService.GetAllCustomerGroups();

            foreach (var shippingMethod in shippingMethods)
            {
                string formKey = "restrict_" + shippingMethod.Id;
                var countryIdsToRestrict = form[formKey].ToString() != null
                    ? form[formKey].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x)
                    .ToList()
                    : new List<string>();

                foreach (var country in countries)
                {

                    bool restrict = countryIdsToRestrict.Contains(country.Id);
                    if (restrict)
                    {
                        if (shippingMethod.RestrictedCountries.FirstOrDefault(c => c.Id == country.Id) == null)
                        {
                            shippingMethod.RestrictedCountries.Add(country);
                            await _shippingMethodService.UpdateShippingMethod(shippingMethod);
                        }
                    }
                    else
                    {
                        if (shippingMethod.RestrictedCountries.FirstOrDefault(c => c.Id == country.Id) != null)
                        {
                            shippingMethod.RestrictedCountries.Remove(shippingMethod.RestrictedCountries.FirstOrDefault(x => x.Id == country.Id));
                            await _shippingMethodService.UpdateShippingMethod(shippingMethod);
                        }
                    }
                }

                formKey = "restrictgroup_" + shippingMethod.Id;
                var roleIdsToRestrict = form[formKey].ToString() != null
                    ? form[formKey].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x)
                    .ToList()
                    : new List<string>();


                foreach (var role in customerGroups)
                {

                    bool restrict = roleIdsToRestrict.Contains(role.Id);
                    if (restrict)
                    {
                        if (shippingMethod.RestrictedGroups.FirstOrDefault(c => c == role.Id) == null)
                        {
                            shippingMethod.RestrictedGroups.Add(role.Id);
                            await _shippingMethodService.UpdateShippingMethod(shippingMethod);
                        }
                    }
                    else
                    {
                        if (shippingMethod.RestrictedGroups.FirstOrDefault(c => c == role.Id) != null)
                        {
                            shippingMethod.RestrictedGroups.Remove(role.Id);
                            await _shippingMethodService.UpdateShippingMethod(shippingMethod);
                        }
                    }
                }
            }

            Success(_translationService.GetResource("Admin.Configuration.Shipping.Restrictions.Updated"));
            //selected tab
            await SaveSelectedTabIndex();

            return RedirectToAction("Restrictions");
        }

        #endregion
    }
}
