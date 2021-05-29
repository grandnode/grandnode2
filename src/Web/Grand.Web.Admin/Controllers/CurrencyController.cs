using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.SharedKernel;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Directory;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Currencies)]
    public partial class CurrencyController : BaseAdminController
    {
        #region Fields

        private readonly ICurrencyService _currencyService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly ICurrencyViewModelService _currencyViewModelService;
        private readonly CurrencySettings _currencySettings;
        private readonly ISettingService _settingService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IStoreService _storeService;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Constructors

        public CurrencyController(ICurrencyService currencyService,
            IExchangeRateService exchangeRateService,
            ICurrencyViewModelService currencyViewModelService,
            CurrencySettings currencySettings, ISettingService settingService,
            IDateTimeService dateTimeService, ITranslationService translationService,
            ILanguageService languageService,
            IStoreService storeService,
            ICacheBase cacheBase)
        {
            _currencyService = currencyService;
            _exchangeRateService = exchangeRateService;
            _currencyViewModelService = currencyViewModelService;
            _currencySettings = currencySettings;
            _settingService = settingService;
            _dateTimeService = dateTimeService;
            _translationService = translationService;
            _languageService = languageService;
            _storeService = storeService;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Methods

        protected async Task ClearCache()
        {
            await _cacheBase.Clear();
        }

        public IActionResult Index() => RedirectToAction("List");

        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(bool liveRates = false)
        {
            if (liveRates)
            {
                try
                {
                    var primaryExchangeCurrency = await _currencyService.GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);
                    if (primaryExchangeCurrency == null)
                        throw new GrandException("Primary exchange rate currency is not set");

                    ViewBag.Rates = await _exchangeRateService.GetCurrencyLiveRates(primaryExchangeCurrency.CurrencyCode);
                }
                catch (Exception exc)
                {
                    Error(exc, false);
                }
            }
            ViewBag.ExchangeRateProviders = new List<SelectListItem>();
            foreach (var erp in _exchangeRateService.LoadAllExchangeRateProviders())
            {
                ViewBag.ExchangeRateProviders.Add(new SelectListItem
                {
                    Text = erp.FriendlyName,
                    Value = erp.SystemName,
                    Selected = erp.SystemName.Equals(_currencySettings.ActiveExchangeRateProviderSystemName, StringComparison.OrdinalIgnoreCase)
                });
            }
            ViewBag.AutoUpdateEnabled = _currencySettings.AutoUpdateEnabled;

            return View();
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> List(IFormCollection formValues)
        {
            _currencySettings.ActiveExchangeRateProviderSystemName = formValues["exchangeRateProvider"];
            _currencySettings.AutoUpdateEnabled = !formValues["autoUpdateEnabled"].Equals("false");
            await _settingService.SaveSetting(_currencySettings);
            return RedirectToAction("List", "Currency");
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> ListGrid(DataSourceRequest command)
        {
            var currenciesModel = (await _currencyService.GetAllCurrencies(true)).Select(x => x.ToModel()).ToList();
            foreach (var currency in currenciesModel)
                currency.IsPrimaryExchangeRateCurrency = currency.Id == _currencySettings.PrimaryExchangeRateCurrencyId;
            foreach (var currency in currenciesModel)
                currency.IsPrimaryStoreCurrency = currency.Id == _currencySettings.PrimaryStoreCurrencyId;

            var gridModel = new DataSourceResult
            {
                Data = currenciesModel,
                Total = currenciesModel.Count
            };
            return Json(gridModel);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ApplyRate(string currencyCode, string rate)
        {
            var _rate = double.Parse(rate, CultureInfo.InvariantCulture.NumberFormat);
            var currency = await _currencyService.GetCurrencyByCode(currencyCode);
            if (currency != null)
            {
                currency.Rate = _rate;
                currency.UpdatedOnUtc = DateTime.UtcNow;
                await _currencyService.UpdateCurrency(currency);
            }
            return Json(new { result = true });
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> MarkAsPrimaryExchangeRateCurrency(string id)
        {
            _currencySettings.PrimaryExchangeRateCurrencyId = id;
            await _settingService.SaveSetting(_currencySettings);

            //now clear cache
            await ClearCache();

            return Json(new { result = true });
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> MarkAsPrimaryStoreCurrency(string id)
        {
            _currencySettings.PrimaryStoreCurrencyId = id;
            await _settingService.SaveSetting(_currencySettings);

            //now clear cache
            await ClearCache();

            return Json(new { result = true });
        }

        #endregion

        #region Create / Edit / Delete

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = _currencyViewModelService.PrepareCurrencyModel();
            //locales
            await AddLocales(_languageService, model.Locales);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(CurrencyModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var currency = await _currencyViewModelService.InsertCurrencyModel(model);
                Success(_translationService.GetResource("Admin.Configuration.Currencies.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = currency.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var currency = await _currencyService.GetCurrencyById(id);
            if (currency == null)
                //No currency found with the specified id
                return RedirectToAction("List");

            var model = currency.ToModel();
            model.CreatedOn = _dateTimeService.ConvertToUserTime(currency.CreatedOnUtc, DateTimeKind.Utc);
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = currency.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(CurrencyModel model, bool continueEditing)
        {
            var currency = await _currencyService.GetCurrencyById(model.Id);
            if (currency == null)
                //No currency found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                //ensure we have at least one published language
                var allCurrencies = await _currencyService.GetAllCurrencies();
                if (allCurrencies.Count == 1 && allCurrencies[0].Id == currency.Id &&
                    !model.Published)
                {
                    Error("At least one published currency is required.");
                    return RedirectToAction("Edit", new { id = currency.Id });
                }
                currency = await _currencyViewModelService.UpdateCurrencyModel(currency, model);
                Success(_translationService.GetResource("Admin.Configuration.Currencies.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = currency.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            model.CreatedOn = _dateTimeService.ConvertToUserTime(currency.CreatedOnUtc, DateTimeKind.Utc);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var currency = await _currencyService.GetCurrencyById(id);
            if (currency == null)
                //No currency found with the specified id
                return RedirectToAction("List");

            try
            {
                if (currency.Id == _currencySettings.PrimaryStoreCurrencyId)
                    throw new GrandException(_translationService.GetResource("Admin.Configuration.Currencies.CantDeletePrimary"));

                if (currency.Id == _currencySettings.PrimaryExchangeRateCurrencyId)
                    throw new GrandException(_translationService.GetResource("Admin.Configuration.Currencies.CantDeleteExchange"));

                //ensure we have at least one published currency
                var allCurrencies = await _currencyService.GetAllCurrencies();
                if (allCurrencies.Count == 1 && allCurrencies[0].Id == currency.Id)
                {
                    Error("At least one published currency is required.");
                    return RedirectToAction("Edit", new { id = currency.Id });
                }
                if (ModelState.IsValid)
                {
                    await _currencyService.DeleteCurrency(currency);

                    Success(_translationService.GetResource("Admin.Configuration.Currencies.Deleted"));
                    return RedirectToAction("List");
                }
                Error(ModelState);
                return RedirectToAction("Edit", new { id = currency.Id });
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("Edit", new { id = currency.Id });
            }
        }

        #endregion
    }
}
