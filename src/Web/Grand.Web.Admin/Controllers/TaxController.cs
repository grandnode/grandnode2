using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Tax;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Web.Admin.Models.Common;
using Grand.Domain.Directory;
using Grand.Business.Common.Interfaces.Logging;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.TaxSettings)]
    public partial class TaxController : BaseAdminController
    {
        #region Fields

        private readonly ITaxService _taxService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ISettingService _settingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheBase _cacheBase;
        private readonly ITranslationService _translationService;
        private readonly ICountryService _countryService;

        #endregion

        #region Constructors

        public TaxController(ITaxService taxService,
            IStoreService storeService,
            IWorkContext workContext,
            ITaxCategoryService taxCategoryService,
            ISettingService settingService, 
            IServiceProvider serviceProvider,
            ICacheBase cacheBase,
            ITranslationService translationService,
            ICountryService countryService)
        {
            _taxService = taxService;
            _storeService = storeService;
            _workContext = workContext;
            _taxCategoryService = taxCategoryService;
            _settingService = settingService;
            _serviceProvider = serviceProvider;
            _cacheBase = cacheBase;
            _translationService = translationService;
            _countryService = countryService;
        }

        #endregion

        #region Tax Providers
        protected async Task ClearCache()
        {
            await _cacheBase.Clear();
        }

        public IActionResult Providers() => View();

        [HttpPost]
        public async Task<IActionResult> Providers(DataSourceRequest command)
        {
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var taxSettings = _settingService.LoadSetting<TaxSettings>(storeScope);
            var taxProviderSettings = _settingService.LoadSetting<TaxProviderSettings>();

            var taxProviders = _taxService.LoadAllTaxProviders()
                .ToList();
            var taxProvidersModel = new List<TaxProviderModel>();
            foreach (var tax in taxProviders)
            {
                var tmp = tax.ToModel();
                var url = tax.ConfigurationUrl;
                if (string.IsNullOrEmpty(url))
                    url = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName.Equals(tax.SystemName, StringComparison.OrdinalIgnoreCase))
                        ?.Instance<IPlugin>(_serviceProvider)?.ConfigurationUrl();
                tmp.ConfigurationUrl = url;

                tmp.IsPrimaryTaxProvider = tmp.SystemName.Equals(taxProviderSettings.ActiveTaxProviderSystemName, StringComparison.OrdinalIgnoreCase);
                taxProvidersModel.Add(tmp);
            }
            var gridModel = new DataSourceResult
            {
                Data = taxProvidersModel,
                Total = taxProvidersModel.Count()
            };

            return Json(gridModel);
        }

        public async Task<IActionResult> MarkAsPrimaryProvider(string systemName)
        {
            var taxProviderettings = _settingService.LoadSetting<TaxProviderSettings>();

            if (String.IsNullOrEmpty(systemName))
            {
                return RedirectToAction("Providers");
            }
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
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var taxSettings = _settingService.LoadSetting<TaxSettings>(storeScope);
            var model = taxSettings.ToModel();

            model.ActiveStore = storeScope;
            model.TaxBasedOnValues = taxSettings.TaxBasedOn.ToSelectList(HttpContext);
            model.TaxDisplayTypeValues = taxSettings.TaxDisplayType.ToSelectList(HttpContext);

            //tax categories
            var taxCategories = await _taxCategoryService.GetAllTaxCategories();
            model.TaxCategories.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Configuration.Tax.Settings.TaxCategories.None"), Value = "" });
            foreach (var tc in taxCategories)
                model.TaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id.ToString()});

            //EU VAT countries
            model.EuVatShopCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.EuVatShopCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = c.Id == taxSettings.EuVatShopCountryId });

            //default tax address
            var defaultAddress = taxSettings.DefaultTaxAddress;
            if (defaultAddress != null)
                model.DefaultTaxAddress = await defaultAddress.ToModel(_countryService);
            else
                model.DefaultTaxAddress = new AddressModel();

            model.DefaultTaxAddress.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.DefaultTaxAddress.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (defaultAddress != null && c.Id == defaultAddress.CountryId) });

            var states = defaultAddress != null && !String.IsNullOrEmpty(defaultAddress.CountryId) ? (await _countryService.GetCountryById(defaultAddress.CountryId))?.StateProvinces : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.DefaultTaxAddress.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == defaultAddress.StateProvinceId) });
            }

            model.DefaultTaxAddress.CountryEnabled = true;
            model.DefaultTaxAddress.StateProvinceEnabled = true;
            model.DefaultTaxAddress.ZipPostalCodeEnabled = true;
            model.DefaultTaxAddress.ZipPostalCodeRequired = true;

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Settings(TaxSettingsModel model, 
            [FromServices] ICustomerActivityService customerActivityService)
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var taxSettings = _settingService.LoadSetting<TaxSettings>(storeScope);
            taxSettings = model.ToEntity(taxSettings);

            await _settingService.SaveSetting(taxSettings, storeScope);

            //now clear cache
            await ClearCache();

            //activity log
            await customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return RedirectToAction("Settings");
        }

        #endregion

        #region Tax Categories

        public IActionResult Categories() => View();

        [HttpPost]
        public async Task<IActionResult> Categories(DataSourceRequest command)
        {
            var categoriesModel = (await _taxCategoryService.GetAllTaxCategories())
                .Select(x => x.ToModel())
                .ToList();
            var gridModel = new DataSourceResult
            {
                Data = categoriesModel,
                Total = categoriesModel.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> CategoryUpdate(TaxCategoryModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var taxCategory = await _taxCategoryService.GetTaxCategoryById(model.Id);
            taxCategory = model.ToEntity(taxCategory);
            await _taxCategoryService.UpdateTaxCategory(taxCategory);

            return new JsonResult("");
        }

        [HttpPost]
        public async Task<IActionResult> CategoryAdd(TaxCategoryModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

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
}
