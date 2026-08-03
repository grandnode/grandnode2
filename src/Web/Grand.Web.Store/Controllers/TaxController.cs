using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Permissions;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Plugins;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Extensions.Mapping.Settings;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Tax;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.TaxSettings)]
public class TaxController(
    ITaxService taxService,
    ITaxCategoryService taxCategoryService,
    ISettingService settingService,
    IServiceProvider serviceProvider,
    ICacheBase cacheBase,
    ITranslationService translationService,
    ICountryService countryService,
    IEnumTranslationService enumTranslationService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    #region Tax Providers

    public IActionResult Providers()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> Providers(DataSourceRequest command)
    {
        var taxProviderSettings = await settingService.LoadSetting<TaxProviderSettings>(CurrentStoreId);
        var taxProvidersModel = new List<TaxProviderModel>();
        foreach (var tax in taxService.LoadAllTaxProviders())
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> MarkAsPrimaryProvider(string systemName)
    {
        var taxProviderSettings = await settingService.LoadSetting<TaxProviderSettings>(CurrentStoreId);
        if (string.IsNullOrEmpty(systemName)) return RedirectToAction("Providers");
        var taxProvider = taxService.LoadTaxProviderBySystemName(systemName);
        if (taxProvider != null)
        {
            taxProviderSettings.ActiveTaxProviderSystemName = systemName;
            await settingService.SaveSetting(taxProviderSettings, CurrentStoreId);
        }
        await cacheBase.Clear();
        return RedirectToAction("Providers");
    }

    #endregion

    #region Settings

    public async Task<IActionResult> Settings()
    {
        var taxSettings = await settingService.LoadSetting<TaxSettings>(CurrentStoreId);
        var model = taxSettings.ToModel();
        model.ActiveStore = CurrentStoreId;
        model.TaxBasedOnValues = enumTranslationService.ToSelectList(taxSettings.TaxBasedOn);
        model.TaxDisplayTypeValues = enumTranslationService.ToSelectList(taxSettings.TaxDisplayType);

        var taxCategories = await taxCategoryService.GetAllTaxCategories(CurrentStoreId);
        model.TaxCategories.Add(new SelectListItem {
            Text = translationService.GetResource("Admin.Configuration.Tax.Settings.TaxCategories.None"), Value = ""
        });
        foreach (var tc in taxCategories)
            model.TaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id });

        model.EuVatShopCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.EuVatShopCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == taxSettings.EuVatShopCountryId });

        var defaultAddress = taxSettings.DefaultTaxAddress;
        model.DefaultTaxAddress = defaultAddress != null
            ? await defaultAddress.ToModel(countryService)
            : new AddressModel();

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
                    { Text = s.Name, Value = s.Id, Selected = s.Id == defaultAddress?.StateProvinceId });

        model.DefaultTaxAddress.CountryEnabled = true;
        model.DefaultTaxAddress.StateProvinceEnabled = true;
        model.DefaultTaxAddress.ZipPostalCodeEnabled = true;
        model.DefaultTaxAddress.ZipPostalCodeRequired = true;

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Settings(TaxSettingsModel model)
    {
        var taxSettings = await settingService.LoadSetting<TaxSettings>(CurrentStoreId);
        taxSettings = model.ToEntity(taxSettings);
        await settingService.SaveSetting(taxSettings, CurrentStoreId);
        await cacheBase.Clear();
        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Settings");
    }

    #endregion

    #region Tax Categories

    public IActionResult Categories()
    {
        return View(new Models.TaxCategoryListModel { StoreId = CurrentStoreId });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> Categories(DataSourceRequest command)
    {
        var categoriesModel = (await taxCategoryService.GetAllTaxCategories(CurrentStoreId))
            .Select(x => x.ToModel())
            .ToList();
        var gridModel = new DataSourceResult {
            Data = categoriesModel,
            Total = categoriesModel.Count
        };
        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CategoryUpdate(TaxCategoryModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
        var taxCategory = await taxCategoryService.GetTaxCategoryById(model.Id);
        if (taxCategory == null || taxCategory.StoreId != CurrentStoreId)
            return new JsonResult("");
        taxCategory = model.ToEntity(taxCategory);
        taxCategory.StoreId = CurrentStoreId;
        await taxCategoryService.UpdateTaxCategory(taxCategory);
        return new JsonResult("");
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> CategoryAdd(TaxCategoryModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
        var taxCategory = new TaxCategory { StoreId = CurrentStoreId };
        taxCategory = model.ToEntity(taxCategory);
        taxCategory.StoreId = CurrentStoreId;
        await taxCategoryService.InsertTaxCategory(taxCategory);
        return new JsonResult("");
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> CategoryDelete(string id)
    {
        var taxCategory = await taxCategoryService.GetTaxCategoryById(id);
        if (taxCategory == null || taxCategory.StoreId != CurrentStoreId)
            return new JsonResult("");
        await taxCategoryService.DeleteTaxCategory(taxCategory);
        return new JsonResult("");
    }

    #endregion
}
