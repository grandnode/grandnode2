using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tax.CountryStateZip.Infrastructure.Cache;
using Tax.CountryStateZip.Services;

namespace Tax.CountryStateZip
{
    public class CountryStateZipTaxProvider : ITaxProvider
    {
        private readonly ITranslationService _translationService;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;
        private readonly ITaxRateService _taxRateService;

        private readonly CountryStateZipTaxSettings _countryStateZipTaxSettings;


        public CountryStateZipTaxProvider(ITranslationService translationService,
            ICacheBase cacheBase,
            IWorkContext workContext,
            ITaxRateService taxRateService,
            CountryStateZipTaxSettings countryStateZipTaxSettings)
        {
            _translationService = translationService;
            _cacheBase = cacheBase;
            _workContext = workContext;
            _taxRateService = taxRateService;
            _countryStateZipTaxSettings = countryStateZipTaxSettings;
        }

        public string ConfigurationUrl => CountryStateZipTaxDefaults.ConfigurationUrl;

        public string SystemName => CountryStateZipTaxDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(CountryStateZipTaxDefaults.FriendlyName);

        public int Priority => _countryStateZipTaxSettings.DisplayOrder;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();


        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public async Task<TaxResult> GetTaxRate(TaxRequest calculateTaxRequest)
        {
            var result = new TaxResult();

            if (calculateTaxRequest.Address == null)
            {
                result.Errors.Add("Address is not set");
                return result;
            }

            const string cacheKey = ModelCacheEventConsumer.ALL_TAX_RATES_MODEL_KEY;
            var allTaxRates = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var taxes = await _taxRateService.GetAllTaxRates();
                return taxes.Select(x => new TaxRateForCaching
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    TaxCategoryId = x.TaxCategoryId,
                    CountryId = x.CountryId,
                    StateProvinceId = x.StateProvinceId,
                    Zip = x.Zip,
                    Percentage = x.Percentage
                });
            });

            var storeId = _workContext.CurrentStore.Id;
            var taxCategoryId = calculateTaxRequest.TaxCategoryId;
            var countryId = calculateTaxRequest.Address.CountryId;
            var stateProvinceId = calculateTaxRequest.Address.StateProvinceId;
            var zip = calculateTaxRequest.Address.ZipPostalCode;

            if (zip == null)
                zip = string.Empty;
            zip = zip.Trim();

            var existingRates = allTaxRates.Where(taxRate => taxRate.CountryId == countryId && taxRate.TaxCategoryId == taxCategoryId).ToList();

            //filter by store
            var matchedByStore = existingRates.Where(taxRate => storeId == taxRate.StoreId).ToList();

            //not found? use the default ones (ID == 0)
            if (!matchedByStore.Any())
                matchedByStore.AddRange(existingRates.Where(taxRate => string.IsNullOrEmpty(taxRate.StoreId)));

            //filter by state/province
            var matchedByStateProvince = matchedByStore.Where(taxRate => stateProvinceId == taxRate.StateProvinceId).ToList();

            //not found? use the default ones (ID == 0)
            if (!matchedByStateProvince.Any())
                matchedByStateProvince.AddRange(matchedByStore.Where(taxRate => string.IsNullOrEmpty(taxRate.StateProvinceId)));

            //filter by zip
            var matchedByZip = matchedByStateProvince.Where(taxRate => (string.IsNullOrEmpty(zip) && string.IsNullOrEmpty(taxRate.Zip)) || zip.Equals(taxRate.Zip, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!matchedByZip.Any())
                matchedByZip.AddRange(matchedByStateProvince.Where(taxRate => string.IsNullOrWhiteSpace(taxRate.Zip)));

            if (matchedByZip.Any())
                result.TaxRate = matchedByZip[0].Percentage;

            return result;
        }

    }
}
