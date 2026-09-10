using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Extensions.Mapping.Settings;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ShippingController(
    IShippingService shippingService,
    IShippingMethodService shippingMethodService,
    ICountryService countryService,
    IGroupService groupService,
    ITranslationService translationService,
    ISettingService settingService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    #region Providers

    public IActionResult Providers()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> Providers(DataSourceRequest command)
    {
        var shippingProviderSettings = await settingService.LoadSetting<ShippingProviderSettings>(CurrentStoreId);
        var shippingProvidersModel = shippingService.LoadAllShippingRateCalculationProviders()
            .Select(p => {
                var m = p.ToModel();
                m.IsActive = p.IsShippingRateMethodActive(shippingProviderSettings);
                return m;
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = shippingProvidersModel,
            Total = shippingProvidersModel.Count
        };
        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProviderUpdate(ShippingRateComputationMethodModel model)
    {
        var shippingProviderSettings = await settingService.LoadSetting<ShippingProviderSettings>(CurrentStoreId);
        var srcm = shippingService.LoadShippingRateCalculationProviderBySystemName(model.SystemName);
        if (srcm == null)
            return new JsonResult("");

        if (srcm.IsShippingRateMethodActive(shippingProviderSettings))
        {
            if (!model.IsActive)
            {
                shippingProviderSettings.ActiveSystemNames.Remove(srcm.SystemName);
                await settingService.SaveSetting(shippingProviderSettings, CurrentStoreId);
            }
        }
        else
        {
            if (model.IsActive)
            {
                shippingProviderSettings.ActiveSystemNames.Add(srcm.SystemName);
                await settingService.SaveSetting(shippingProviderSettings, CurrentStoreId);
            }
        }
        return new JsonResult("");
    }

    #endregion

    #region Shipping settings

    public async Task<IActionResult> Settings()
    {
        var shippingSettings = await settingService.LoadSetting<ShippingSettings>(CurrentStoreId);
        var model = shippingSettings.ToModel();
        model.ActiveStore = CurrentStoreId;

        var originAddress = shippingSettings.ShippingOriginAddress;
        model.ShippingOriginAddress = originAddress != null
            ? await originAddress.ToModel(countryService)
            : new AddressModel();

        model.ShippingOriginAddress.AvailableCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.ShippingOriginAddress.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = originAddress != null && c.Id == originAddress.CountryId });

        var states = originAddress != null && !string.IsNullOrEmpty(originAddress.CountryId)
            ? (await countryService.GetCountryById(originAddress.CountryId))?.StateProvinces ?? []
            : new List<StateProvince>();
        var selectedStateProvinceId = originAddress?.StateProvinceId;
        if (states?.Count > 0)
            foreach (var s in states)
                model.ShippingOriginAddress.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == selectedStateProvinceId });

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
        var storeScope = CurrentStoreId;
        var shippingSettings = await settingService.LoadSetting<ShippingSettings>(storeScope);
        shippingSettings = model.ToEntity(shippingSettings);
        await settingService.SaveSetting(shippingSettings, storeScope);
        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Settings");
    }

    #endregion

    #region Restrictions

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Restrictions()
    {
        var model = new ShippingMethodRestrictionModel();

        var countries = await countryService.GetAllCountries(showHidden: true);
        var shippingMethods = await shippingMethodService.GetAllShippingMethods(storeId: CurrentStoreId);
        var customerGroups = await groupService.GetAllCustomerGroups();

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
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RestrictionSave(IDictionary<string, string[]> model)
    {
        var countries = await countryService.GetAllCountries(showHidden: true);
        var shippingMethods = await shippingMethodService.GetAllShippingMethods(storeId: CurrentStoreId);
        var customerGroups = await groupService.GetAllCustomerGroups();
        //GetAllShippingMethods also returns global (StoreId=="") shipping methods, shared by every store;
        //only mutate restrictions on methods this store manager exclusively owns.
        foreach (var shippingMethod in shippingMethods.Where(x => x.StoreId == CurrentStoreId))
        {
            await SaveRestrictedCountries(model, shippingMethod, countries);
            await SaveRestrictedGroup(model, shippingMethod, customerGroups);
        }

        Success(translationService.GetResource("Admin.Configuration.Shipping.Restrictions.Updated"));
        await SaveSelectedTabIndex();

        return RedirectToAction("Restrictions");
    }

    private async Task SaveRestrictedGroup(IDictionary<string, string[]> model, ShippingMethod shippingMethod,
        IPagedList<CustomerGroup> customerGroups)
    {
        if (model.TryGetValue($"restrictgroup_{shippingMethod.Id}", out var roleIds))
        {
            var roleIdsToRestrict = roleIds.ToList();
            var changed = false;
            foreach (var role in customerGroups)
            {
                var restrict = roleIdsToRestrict.Contains(role.Id);
                var alreadyRestricted = shippingMethod.RestrictedGroups.Contains(role.Id);
                if (restrict && !alreadyRestricted)
                {
                    shippingMethod.RestrictedGroups.Add(role.Id);
                    changed = true;
                }
                else if (!restrict && alreadyRestricted)
                {
                    shippingMethod.RestrictedGroups.Remove(role.Id);
                    changed = true;
                }
            }
            if (changed)
                await shippingMethodService.UpdateShippingMethod(shippingMethod);
        }
        else
        {
            if (shippingMethod.RestrictedGroups.Count > 0)
            {
                shippingMethod.RestrictedGroups.Clear();
                await shippingMethodService.UpdateShippingMethod(shippingMethod);
            }
        }
    }

    private async Task SaveRestrictedCountries(IDictionary<string, string[]> model, ShippingMethod shippingMethod,
        IList<Country> countries)
    {
        if (model.TryGetValue($"restrict_{shippingMethod.Id}", out var countryIds))
        {
            var countryIdsToRestrict = countryIds.ToList();
            var changed = false;
            foreach (var country in countries)
            {
                var restrict = countryIdsToRestrict.Contains(country.Id);
                var alreadyRestricted = shippingMethod.RestrictedCountries.Any(c => c.Id == country.Id);
                if (restrict && !alreadyRestricted)
                {
                    shippingMethod.RestrictedCountries.Add(country);
                    changed = true;
                }
                else if (!restrict && alreadyRestricted)
                {
                    shippingMethod.RestrictedCountries.Remove(
                        shippingMethod.RestrictedCountries.First(c => c.Id == country.Id));
                    changed = true;
                }
            }
            if (changed)
                await shippingMethodService.UpdateShippingMethod(shippingMethod);
        }
        else
        {
            if (shippingMethod.RestrictedCountries.Count > 0)
            {
                shippingMethod.RestrictedCountries.Clear();
                await shippingMethodService.UpdateShippingMethod(shippingMethod);
            }
        }
    }

    #endregion
}
