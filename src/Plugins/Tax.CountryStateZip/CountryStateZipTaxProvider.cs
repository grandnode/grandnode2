using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Infrastructure.Caching;
using Tax.CountryStateZip.Infrastructure.Cache;
using Tax.CountryStateZip.Services;

namespace Tax.CountryStateZip;

public class CountryStateZipTaxProvider : ITaxProvider
{
    private readonly ICacheBase _cacheBase;

    private readonly CountryStateZipTaxSettings _countryStateZipTaxSettings;
    private readonly ITaxRateService _taxRateService;
    private readonly ITranslationService _translationService;


    public CountryStateZipTaxProvider(ITranslationService translationService,
        ICacheBase cacheBase,
        ITaxRateService taxRateService,
        CountryStateZipTaxSettings countryStateZipTaxSettings)
    {
        _translationService = translationService;
        _cacheBase = cacheBase;
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
    ///     Gets tax rate
    /// </summary>
    /// <param name="calculateTaxRequest">Tax calculation request</param>
    /// <returns>Tax</returns>
    public async Task<TaxResult> GetTaxRate(TaxRequest calculateTaxRequest)
    {
        if (calculateTaxRequest.Address == null)
        {
            var errorResult = new TaxResult();
            errorResult.Errors.Add("Address is not set");
            return errorResult;
        }

        var allTaxRates = await GetAllTaxRatesFromCache();

        var address = calculateTaxRequest.Address;
        var storeId = calculateTaxRequest.Store?.Id ?? string.Empty;
        var zip = address.ZipPostalCode?.Trim() ?? string.Empty;

        var byCountryAndCategory = allTaxRates
            .Where(r => r.CountryId == address.CountryId && r.TaxCategoryId == calculateTaxRequest.TaxCategoryId)
            .ToList();

        var byStore = MatchOrFallback(byCountryAndCategory,
            r => r.StoreId == storeId,
            r => string.IsNullOrEmpty(r.StoreId));

        var byStateProvince = MatchOrFallback(byStore,
            r => r.StateProvinceId == address.StateProvinceId,
            r => string.IsNullOrEmpty(r.StateProvinceId));

        var matched = byStateProvince
            .OrderByDescending(r => !string.IsNullOrWhiteSpace(r.Zip))
            .FirstOrDefault(r =>
                string.IsNullOrWhiteSpace(r.Zip) ||
                (!string.IsNullOrEmpty(zip) && zip.Equals(r.Zip, StringComparison.OrdinalIgnoreCase)));

        return new TaxResult { TaxRate = matched?.Percentage ?? 0 };
    }

    private async Task<List<TaxRateForCaching>> GetAllTaxRatesFromCache()
    {
        return await _cacheBase.GetAsync(
            ModelCacheEventConsumer.ALL_TAX_RATES_MODEL_KEY,
            async () => (await _taxRateService.GetAllTaxRates())
                .Select(x => new TaxRateForCaching {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    TaxCategoryId = x.TaxCategoryId,
                    CountryId = x.CountryId,
                    StateProvinceId = x.StateProvinceId,
                    Zip = x.Zip,
                    Percentage = x.Percentage
                }).ToList());
    }

    private static List<TaxRateForCaching> MatchOrFallback(
        List<TaxRateForCaching> source,
        Func<TaxRateForCaching, bool> exact,
        Func<TaxRateForCaching, bool> fallback)
    {
        List<TaxRateForCaching> matched = [..source.Where(exact)];
        return matched.Count > 0 ? matched : [..source.Where(fallback)];
    }
}
