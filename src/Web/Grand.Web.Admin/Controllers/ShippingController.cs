using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Extensions.Mapping.Settings;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ShippingController : BaseAdminController
{
    #region Constructors

    public ShippingController(
        IShippingService shippingService,
        IShippingMethodService shippingMethodService,
        ISettingService settingService,
        ICountryService countryService,
        ITranslationService translationService,
        IStoreService storeService,
        IGroupService groupService)
    {
        _shippingService = shippingService;
        _shippingMethodService = shippingMethodService;
        _settingService = settingService;
        _countryService = countryService;
        _translationService = translationService;
        _storeService = storeService;
        _groupService = groupService;
    }

    #endregion

    #region Fields

    private readonly IShippingService _shippingService;
    private readonly IShippingMethodService _shippingMethodService;
    private readonly ISettingService _settingService;
    private readonly ICountryService _countryService;
    private readonly ITranslationService _translationService;
    private readonly IStoreService _storeService;
    private readonly IGroupService _groupService;

    #endregion

    #region Shipping rate  methods

    public IActionResult Providers()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Providers(DataSourceRequest command)
    {
        var storeScope = await GetActiveStore();

        var _shippingProviderSettings = await _settingService.LoadSetting<ShippingProviderSettings>(storeScope);
        var shippingProvidersModel = new List<ShippingRateComputationMethodModel>();
        var shippingProviders = _shippingService.LoadAllShippingRateCalculationProviders();
        foreach (var shippingProvider in shippingProviders)
        {
            var tmp1 = shippingProvider.ToModel();
            tmp1.IsActive = shippingProvider.IsShippingRateMethodActive(_shippingProviderSettings);
            shippingProvidersModel.Add(tmp1);
        }

        shippingProvidersModel = shippingProvidersModel.ToList();
        var gridModel = new DataSourceResult {
            Data = shippingProvidersModel,
            Total = shippingProvidersModel.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> ProviderUpdate(ShippingRateComputationMethodModel model)
    {
        var storeScope = await GetActiveStore();

        var _shippingProviderSettings = await _settingService.LoadSetting<ShippingProviderSettings>(storeScope);

        var srcm = _shippingService.LoadShippingRateCalculationProviderBySystemName(model.SystemName);
        if (srcm.IsShippingRateMethodActive(_shippingProviderSettings))
        {
            if (!model.IsActive)
            {
                //mark as disabled
                _shippingProviderSettings.ActiveSystemNames.Remove(srcm.SystemName);
                await _settingService.SaveSetting(_shippingProviderSettings, storeScope);
            }
        }
        else
        {
            if (model.IsActive)
            {
                //mark as active
                _shippingProviderSettings.ActiveSystemNames.Add(srcm.SystemName);
                await _settingService.SaveSetting(_shippingProviderSettings, storeScope);
            }
        }

        return new JsonResult("");
    }

    #endregion

    #region Shipping Settings

    public async Task<IActionResult> Settings()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var shippingSettings = await _settingService.LoadSetting<ShippingSettings>(storeScope);
        var model = shippingSettings.ToModel();
        model.ActiveStore = storeScope;

        //shipping origin
        var originAddress = shippingSettings.ShippingOriginAddress;
        if (originAddress != null)
            model.ShippingOriginAddress = await originAddress.ToModel(_countryService);
        else
            model.ShippingOriginAddress = new AddressModel();

        model.ShippingOriginAddress.AvailableCountries.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await _countryService.GetAllCountries(showHidden: true))
            model.ShippingOriginAddress.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = originAddress != null && c.Id == originAddress.CountryId });

        var states = originAddress != null && !string.IsNullOrEmpty(originAddress.CountryId)
            ? (await _countryService.GetCountryById(originAddress.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states?.Count > 0)
            foreach (var s in states)
                model.ShippingOriginAddress.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == originAddress.StateProvinceId });

        model.ShippingOriginAddress.CountryEnabled = true;
        model.ShippingOriginAddress.StateProvinceEnabled = true;
        model.ShippingOriginAddress.CityEnabled = true;
        model.ShippingOriginAddress.StreetAddressEnabled = true;
        model.ShippingOriginAddress.ZipPostalCodeEnabled = true;
        model.ShippingOriginAddress.ZipPostalCodeRequired = true;
        model.ShippingOriginAddress.AddressTypeEnabled = false;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(ShippingSettingsModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var shippingSettings = await _settingService.LoadSetting<ShippingSettings>(storeScope);
        shippingSettings = model.ToEntity(shippingSettings);

        await _settingService.SaveSetting(shippingSettings, storeScope);

        Success(_translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Settings");
    }

    #endregion

    #region Restrictions

    public async Task<IActionResult> Restrictions(string storeId = "")
    {
        var model = new ShippingMethodRestrictionModel();

        var stores = await _storeService.GetAllStores();
        model.StoreId = storeId;
        model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in stores)
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id, Selected = s.Id == storeId });

        var countries = await _countryService.GetAllCountries(showHidden: true);
        var shippingMethods = await _shippingMethodService.GetAllShippingMethods(storeId);
        var customerGroups = await _groupService.GetAllCustomerGroups();

        foreach (var country in countries)
            model.AvailableCountries.Add(new CountryModel {
                Id = country.Id,
                Name = country.Name
            });
        foreach (var sm in shippingMethods)
            model.AvailableShippingMethods.Add(new ShippingMethodModel {
                Id = sm.Id,
                Name = sm.Name
            });
        foreach (var r in customerGroups)
            model.AvailableCustomerGroups.Add(new CustomerGroupModel { Id = r.Id, Name = r.Name });

        foreach (var country in countries)
        foreach (var shippingMethod in shippingMethods)
        {
            var restricted = shippingMethod.CountryRestrictionExists(country.Id);
            if (!model.Restricted.ContainsKey(country.Id))
                model.Restricted[country.Id] = new Dictionary<string, bool>();
            model.Restricted[country.Id][shippingMethod.Id] = restricted;
        }

        foreach (var role in customerGroups)
        foreach (var shippingMethod in shippingMethods)
        {
            var restricted = shippingMethod.CustomerGroupRestrictionExists(role.Id);
            if (!model.RestictedGroup.ContainsKey(role.Id))
                model.RestictedGroup[role.Id] = new Dictionary<string, bool>();
            model.RestictedGroup[role.Id][shippingMethod.Id] = restricted;
        }


        return View(model);
    }

    [HttpPost]
    [ActionName("Restrictions")]
    [RequestFormLimits(ValueCountLimit = 2048)]
    public async Task<IActionResult> RestrictionSave(IDictionary<string, string[]> model, string storeId = "")
    {
        var countries = await _countryService.GetAllCountries(showHidden: true);
        var shippingMethods = await _shippingMethodService.GetAllShippingMethods(storeId);
        var customerGroups = await _groupService.GetAllCustomerGroups();
        foreach (var shippingMethod in shippingMethods)
        {
            await SaveRestrictedCountries(model, shippingMethod, countries);
            await SaveRestrictedGroup(model, shippingMethod, customerGroups);
        }

        Success(_translationService.GetResource("Admin.Configuration.Shipping.Restrictions.Updated"));
        //selected tab
        await SaveSelectedTabIndex();

        return RedirectToAction("Restrictions", new { storeId });
    }

    private async Task SaveRestrictedGroup(IDictionary<string, string[]> model, ShippingMethod shippingMethod,
        IPagedList<CustomerGroup> customerGroups)
    {
        if (model.TryGetValue($"restrictgroup_{shippingMethod.Id}", out var roleIds))
        {
            var roleIdsToRestrict = roleIds.ToList();
            foreach (var role in customerGroups)
            {
                var restrict = roleIdsToRestrict.Contains(role.Id);
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
        else
        {
            shippingMethod.RestrictedGroups.Clear();
            await _shippingMethodService.UpdateShippingMethod(shippingMethod);
        }
    }

    private async Task SaveRestrictedCountries(IDictionary<string, string[]> model, ShippingMethod shippingMethod,
        IList<Country> countries)
    {
        if (model.TryGetValue($"restrict_{shippingMethod.Id}", out var countryIds))
        {
            var countryIdsToRestrict = countryIds.ToList();
            foreach (var country in countries)
            {
                var restrict = countryIdsToRestrict.Contains(country.Id);
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
                        shippingMethod.RestrictedCountries.Remove(
                            shippingMethod.RestrictedCountries.FirstOrDefault(x => x.Id == country.Id));
                        await _shippingMethodService.UpdateShippingMethod(shippingMethod);
                    }
                }
            }
        }
        else
        {
            shippingMethod.RestrictedCountries.Clear();
            await _shippingMethodService.UpdateShippingMethod(shippingMethod);
        }
    }

    #endregion
}