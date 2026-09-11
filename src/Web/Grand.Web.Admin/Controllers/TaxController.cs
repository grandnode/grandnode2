using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Directory;
using Grand.Domain.Tax;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Extensions.Mapping.Settings;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Tax;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Controllers;

// Attributes are restated here (and on Store's TaxController) because they used to arrive
// transitively via BaseAdminController/BaseStoreController - BaseTaxCategoryController (used
// instead now) can't inherit either, since it must stay host-agnostic: Admin, Store, and
// (hypothetically) Vendor all extend it.
[AuthorizeAdmin]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class TaxController(
    ITaxService taxService,
    ITaxCategoryService taxCategoryService,
    ISettingService settingService,
    IServiceProvider serviceProvider,
    ICacheBase cacheBase,
    ITranslationService translationService,
    ICountryService countryService,
    IStoreService storeService,
    IEnumTranslationService enumTranslationService,
    IAdminStoreService adminStoreService,
    IAdminDataScope<TaxCategory> scope)
    : BaseTaxCategoryController(taxCategoryService, storeService, translationService, scope)
{
    // ARCH-001 GetActiveStore() consolidation: was a hand-duplicated private copy of the same logic
    // now in IAdminStoreService (also used by BaseAdminController and StoreScope) - BaseTaxCategoryController
    // can't carry it itself, it must stay host-agnostic (Admin/Store/Vendor all extend it).
    private Task<string> GetActiveStore() => adminStoreService.GetActiveStore();

    #region Tax Providers

    protected async Task ClearCache()
    {
        await cacheBase.Clear();
    }

    public IActionResult Providers()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Providers(DataSourceRequest command)
    {
        var storeScope = await GetActiveStore();
        var taxProviderSettings = await settingService.LoadSetting<TaxProviderSettings>(storeScope);

        var taxProviders = taxService.LoadAllTaxProviders()
            .ToList();
        var taxProvidersModel = new List<TaxProviderModel>();
        foreach (var tax in taxProviders)
        {
            var tmp = tax.ToModel();
            var url = tax.ConfigurationUrl;
            if (string.IsNullOrEmpty(url))
                url = PluginManager.ReferencedPlugins.FirstOrDefault(x =>
                        x.SystemName.Equals(tax.SystemName, StringComparison.OrdinalIgnoreCase))
                    ?.Instance<IPlugin>(serviceProvider)?.ConfigurationUrl();
            tmp.ConfigurationUrl = url;

            tmp.IsPrimaryTaxProvider = tmp.SystemName.Equals(taxProviderSettings.ActiveTaxProviderSystemName,
                StringComparison.OrdinalIgnoreCase);
            taxProvidersModel.Add(tmp);
        }

        var gridModel = new DataSourceResult {
            Data = taxProvidersModel,
            Total = taxProvidersModel.Count
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> MarkAsPrimaryProvider(string systemName)
    {
        var storeScope = await GetActiveStore();
        var taxProviderettings = await settingService.LoadSetting<TaxProviderSettings>(storeScope);

        if (string.IsNullOrEmpty(systemName)) return RedirectToAction("Providers");
        var taxProvider = taxService.LoadTaxProviderBySystemName(systemName);
        if (taxProvider != null)
        {
            taxProviderettings.ActiveTaxProviderSystemName = systemName;
            await settingService.SaveSetting(taxProviderettings, storeScope);
        }

        //now clear cache
        await ClearCache();

        return RedirectToAction("Providers");
    }

    #endregion

    #region Settings

    public async Task<IActionResult> Settings()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var taxSettings = await settingService.LoadSetting<TaxSettings>(storeScope);
        var model = taxSettings.ToModel();

        model.ActiveStore = storeScope;
        model.TaxBasedOnValues = enumTranslationService.ToSelectList(taxSettings.TaxBasedOn);
        model.TaxDisplayTypeValues = enumTranslationService.ToSelectList(taxSettings.TaxDisplayType);

        //tax categories
        var taxCategories = await taxCategoryService.GetAllTaxCategories();
        model.TaxCategories.Add(new SelectListItem {
            Text = translationService.GetResource("Admin.Configuration.Tax.Settings.TaxCategories.None"), Value = ""
        });
        foreach (var tc in taxCategories)
            model.TaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id });

        //EU VAT countries
        model.EuVatShopCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.EuVatShopCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == taxSettings.EuVatShopCountryId });

        //default tax address
        var defaultAddress = taxSettings.DefaultTaxAddress;
        if (defaultAddress != null)
            model.DefaultTaxAddress = await defaultAddress.ToModel(countryService);
        else
            model.DefaultTaxAddress = new AddressModel();

        model.DefaultTaxAddress.AvailableCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.DefaultTaxAddress.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = defaultAddress != null && c.Id == defaultAddress.CountryId });

        var states = defaultAddress != null && !string.IsNullOrEmpty(defaultAddress.CountryId)
            ? (await countryService.GetCountryById(defaultAddress.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states?.Count > 0)
            foreach (var s in states)
                model.DefaultTaxAddress.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == defaultAddress.StateProvinceId });

        model.DefaultTaxAddress.CountryEnabled = true;
        model.DefaultTaxAddress.StateProvinceEnabled = true;
        model.DefaultTaxAddress.ZipPostalCodeEnabled = true;
        model.DefaultTaxAddress.ZipPostalCodeRequired = true;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(TaxSettingsModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var taxSettings = await settingService.LoadSetting<TaxSettings>(storeScope);
        taxSettings = model.ToEntity(taxSettings);

        await settingService.SaveSetting(taxSettings, storeScope);

        //now clear cache
        await ClearCache();

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Settings");
    }

    #endregion

    #region Tax Categories

    public async Task<IActionResult> Categories()
    {
        var model = new TaxCategoryListModel();
        model.AvailableStores.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in await storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
        return View(model);
    }

    #endregion
}
