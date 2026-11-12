using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     The return methods a store offers - the active return shipping providers - with the address the parcels are
///     sent to and the cost of sending one on your own. The settings are kept per store; each panel passes the store
///     it works in
/// </summary>
[SharedViewFolder("ReturnShipping")]
public abstract class BaseReturnShippingController(
    IReturnShippingService returnShippingService,
    ISettingService settingService,
    ICountryService countryService,
    IRequestTranslationService translationService) : BaseController
{
    private const string PageView = "Providers";

    /// <summary>
    ///     The link to the provider's configuration screen in this panel, or null when there is none
    /// </summary>
    protected virtual string ConfigurationUrl(IReturnShippingProvider provider)
    {
        return provider.ConfigurationUrl;
    }

    protected virtual async Task<IActionResult> ProvidersCore(string storeId)
    {
        var settings = await settingService.LoadSetting<ReturnShippingProviderSettings>(storeId);
        var model = returnShippingService.LoadAllProviders(storeId: storeId)
            .Select(provider => new ReturnShippingProviderModel {
                FriendlyName = provider.FriendlyName,
                SystemName = provider.SystemName,
                DisplayOrder = provider.Priority,
                IsActive = IsActive(settings, provider.SystemName),
                ConfigurationUrl = ConfigurationUrl(provider)
            })
            .ToList();

        return Json(new DataSourceResult { Data = model, Total = model.Count });
    }

    protected virtual async Task<IActionResult> ProviderUpdateCore(ReturnShippingProviderModel model, string storeId)
    {
        //only a provider offered in the store can be switched on or off there
        var provider = returnShippingService.LoadAllProviders(storeId: storeId)
            .FirstOrDefault(x => string.Equals(x.SystemName, model.SystemName, StringComparison.OrdinalIgnoreCase));
        if (provider == null)
            return new JsonResult("");

        var settings = await settingService.LoadSetting<ReturnShippingProviderSettings>(storeId);
        if (IsActive(settings, provider.SystemName) == model.IsActive)
            return new JsonResult("");

        if (model.IsActive)
            settings.ActiveSystemNames.Add(provider.SystemName);
        else
            settings.ActiveSystemNames.RemoveAll(x =>
                string.Equals(x, provider.SystemName, StringComparison.OrdinalIgnoreCase));
        await settingService.SaveSetting(settings, storeId);

        return new JsonResult("");
    }

    protected virtual async Task<IActionResult> SettingsCore(string storeId)
    {
        var settings = await settingService.LoadSetting<ReturnShippingProviderSettings>(storeId);
        var model = new ReturnShippingSettingsModel {
            ActiveStore = storeId,
            SendYourselfCost = settings.SendYourselfCost,
            ReturnAddress = settings.ReturnAddress != null
                ? await settings.ReturnAddress.ToModel(countryService)
                : new AddressModel()
        };
        await PrepareAddress(model.ReturnAddress);

        return View(PageView, model);
    }

    protected virtual async Task<IActionResult> SettingsSaveCore(ReturnShippingSettingsModel model, string storeId)
    {
        model.ReturnAddress ??= new AddressModel();
        //the address is optional - without one the returns go to the shipping origin - so only the cost is checked
        if (model.SendYourselfCost < 0)
        {
            ModelState.AddModelError(nameof(model.SendYourselfCost),
                translationService.GetResource("Admin.Configuration.Shipping.ReturnMethods.SendYourselfCost.Negative"));
            model.ActiveStore = storeId;
            await PrepareAddress(model.ReturnAddress);
            return View(PageView, model);
        }

        var settings = await settingService.LoadSetting<ReturnShippingProviderSettings>(storeId);
        settings.SendYourselfCost = model.SendYourselfCost;
        settings.ReturnAddress = model.ReturnAddress.ToEntity(settings.ReturnAddress ?? new Address());
        await settingService.SaveSetting(settings, storeId);

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Providers");
    }

    private static bool IsActive(ReturnShippingProviderSettings settings, string systemName)
    {
        return settings.ActiveSystemNames.Contains(systemName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     The fields of the address editor, as in the shipping origin, with the company, the person and the phone a
    ///     carrier puts on the label
    /// </summary>
    private async Task PrepareAddress(AddressModel address)
    {
        address.AvailableCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            address.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == address.CountryId });

        var states = !string.IsNullOrEmpty(address.CountryId)
            ? (await countryService.GetCountryById(address.CountryId))?.StateProvinces
            : null;
        if (states?.Count > 0)
            foreach (var s in states)
                address.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == address.StateProvinceId });

        address.CompanyEnabled = true;
        address.FirstNameEnabled = true;
        address.LastNameEnabled = true;
        address.PhoneEnabled = true;
        address.CountryEnabled = true;
        address.StateProvinceEnabled = true;
        address.CityEnabled = true;
        address.StreetAddressEnabled = true;
        address.StreetAddress2Enabled = true;
        address.ZipPostalCodeEnabled = true;
        address.AddressTypeEnabled = false;
    }
}
