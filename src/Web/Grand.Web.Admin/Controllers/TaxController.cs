using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Directory;
using Grand.Domain.Tax;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Extensions.Mapping.Settings;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Tax;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.TaxSettings)]
public class TaxController : BaseAdminController
{
    #region Constructors

    public TaxController(ITaxService taxService,
        ITaxCategoryService taxCategoryService,
        ISettingService settingService,
        IServiceProvider serviceProvider,
        ICacheBase cacheBase,
        ITranslationService translationService,
        ICountryService countryService)
    {
        _taxService = taxService;
        _taxCategoryService = taxCategoryService;
        _settingService = settingService;
        _serviceProvider = serviceProvider;
        _cacheBase = cacheBase;
        _translationService = translationService;
        _countryService = countryService;
    }

    #endregion

    #region Fields

    private readonly ITaxService _taxService;
    private readonly ITaxCategoryService _taxCategoryService;
    private readonly ISettingService _settingService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICacheBase _cacheBase;
    private readonly ITranslationService _translationService;
    private readonly ICountryService _countryService;

    #endregion

    #region Tax Providers

    protected async Task ClearCache()
    {
        await _cacheBase.Clear();
    }

    public IActionResult Providers()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Providers(DataSourceRequest command)
    {
        var storeScope = await GetActiveStore();
        _settingService.LoadSetting<TaxSettings>(storeScope);
        var taxProviderSettings = _settingService.LoadSetting<TaxProviderSettings>();

        var taxProviders = _taxService.LoadAllTaxProviders()
            .ToList();
        var taxProvidersModel = new List<TaxProviderModel>();
        foreach (var tax in taxProviders)
        {
            var tmp = tax.ToModel();
            var url = tax.ConfigurationUrl;
            if (string.IsNullOrEmpty(url))
                url = PluginManager.ReferencedPlugins.FirstOrDefault(x =>
                        x.SystemName.Equals(tax.SystemName, StringComparison.OrdinalIgnoreCase))
                    ?.Instance<IPlugin>(_serviceProvider)?.ConfigurationUrl();
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
        var taxProviderettings = _settingService.LoadSetting<TaxProviderSettings>();

        if (string.IsNullOrEmpty(systemName)) return RedirectToAction("Providers");
        var taxProvider = _taxService.LoadTaxProviderBySystemName(systemName);
        if (taxProvider != null)
        {
            taxProviderettings.ActiveTaxProviderSystemName = systemName;
            await _settingService.SaveSetting(taxProviderettings);
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
        var taxSettings = _settingService.LoadSetting<TaxSettings>(storeScope);
        var model = taxSettings.ToModel();

        model.ActiveStore = storeScope;
        model.TaxBasedOnValues = taxSettings.TaxBasedOn.ToSelectList(HttpContext);
        model.TaxDisplayTypeValues = taxSettings.TaxDisplayType.ToSelectList(HttpContext);

        //tax categories
        var taxCategories = await _taxCategoryService.GetAllTaxCategories();
        model.TaxCategories.Add(new SelectListItem {
            Text = _translationService.GetResource("Admin.Configuration.Tax.Settings.TaxCategories.None"), Value = ""
        });
        foreach (var tc in taxCategories)
            model.TaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id });

        //EU VAT countries
        model.EuVatShopCountries.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await _countryService.GetAllCountries(showHidden: true))
            model.EuVatShopCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == taxSettings.EuVatShopCountryId });

        //default tax address
        var defaultAddress = taxSettings.DefaultTaxAddress;
        if (defaultAddress != null)
            model.DefaultTaxAddress = await defaultAddress.ToModel(_countryService);
        else
            model.DefaultTaxAddress = new AddressModel();

        model.DefaultTaxAddress.AvailableCountries.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await _countryService.GetAllCountries(showHidden: true))
            model.DefaultTaxAddress.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = defaultAddress != null && c.Id == defaultAddress.CountryId });

        var states = defaultAddress != null && !string.IsNullOrEmpty(defaultAddress.CountryId)
            ? (await _countryService.GetCountryById(defaultAddress.CountryId))?.StateProvinces
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
        var taxSettings = _settingService.LoadSetting<TaxSettings>(storeScope);
        taxSettings = model.ToEntity(taxSettings);

        await _settingService.SaveSetting(taxSettings, storeScope);

        //now clear cache
        await ClearCache();

        Success(_translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Settings");
    }

    #endregion

    #region Tax Categories

    public IActionResult Categories()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Categories(DataSourceRequest command)
    {
        var categoriesModel = (await _taxCategoryService.GetAllTaxCategories())
            .Select(x => x.ToModel())
            .ToList();
        var gridModel = new DataSourceResult {
            Data = categoriesModel,
            Total = categoriesModel.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> CategoryUpdate(TaxCategoryModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

        var taxCategory = await _taxCategoryService.GetTaxCategoryById(model.Id);
        taxCategory = model.ToEntity(taxCategory);
        await _taxCategoryService.UpdateTaxCategory(taxCategory);

        return new JsonResult("");
    }

    [HttpPost]
    public async Task<IActionResult> CategoryAdd(TaxCategoryModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

        var taxCategory = new TaxCategory();
        taxCategory = model.ToEntity(taxCategory);
        await _taxCategoryService.InsertTaxCategory(taxCategory);

        return new JsonResult("");
    }

    [HttpPost]
    public async Task<IActionResult> CategoryDelete(string id)
    {
        var taxCategory = await _taxCategoryService.GetTaxCategoryById(id);
        if (taxCategory == null)
            throw new ArgumentException("No tax category found with the specified id");
        await _taxCategoryService.DeleteTaxCategory(taxCategory);

        return new JsonResult("");
    }

    #endregion
}