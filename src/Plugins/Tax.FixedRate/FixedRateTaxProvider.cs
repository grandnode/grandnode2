using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tax.FixedRate.Models;

namespace Tax.FixedRate
{
    public class FixedRateTaxProvider : ITaxProvider
    {
        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;

        public FixedRateTaxProvider(ISettingService settingService, ITranslationService translationService)
        {
            _settingService = settingService;
            _translationService = translationService;
        }

        public string ConfigurationUrl => FixedRateTaxDefaults.ConfigurationUrl;

        public string SystemName => FixedRateTaxDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(FixedRateTaxDefaults.FriendlyName);

        public int Priority => 0;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public Task<TaxResult> GetTaxRate(TaxRequest calculateTaxRequest)
        {
            var result = new TaxResult
            {
                TaxRate = GetTaxRate(calculateTaxRequest.TaxCategoryId)
            };
            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets a tax rate
        /// </summary>
        /// <param name="taxCategoryId">The tax category identifier</param>
        /// <returns>Tax rate</returns>
        protected double GetTaxRate(string taxCategoryId)
        {
            var rate = _settingService.GetSettingByKey<FixedTaxRate>(string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", taxCategoryId))?.Rate;
            return rate ?? 0;
        }

    }
}
