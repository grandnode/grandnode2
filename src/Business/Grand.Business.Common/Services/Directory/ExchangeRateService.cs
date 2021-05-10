using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Providers;
using Grand.Domain.Directory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.Directory
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly IEnumerable<IExchangeRateProvider> _exchangeRateProvider;
        private readonly CurrencySettings _currencySettings;


        public ExchangeRateService(IEnumerable<IExchangeRateProvider> exchangeRateProvider, CurrencySettings currencySettings)
        {
            _exchangeRateProvider = exchangeRateProvider;
            _currencySettings = currencySettings;
        }

        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        public virtual Task<IList<ExchangeRate>> GetCurrencyLiveRates(string exchangeRateCurrencyCode)
        {
            var exchangeRateProvider = LoadActiveExchangeRateProvider();
            if (exchangeRateProvider == null)
                throw new Exception("Active exchange rate provider cannot be loaded");
            return exchangeRateProvider.GetCurrencyLiveRates(exchangeRateCurrencyCode);
        }


        /// <summary>
        /// Load active exchange rate provider
        /// </summary>
        /// <returns>Active exchange rate provider</returns>
        public virtual IExchangeRateProvider LoadActiveExchangeRateProvider()
        {
            var exchangeRateProvider = LoadExchangeRateProviderBySystemName(_currencySettings.ActiveExchangeRateProviderSystemName);
            if (exchangeRateProvider == null)
                exchangeRateProvider = LoadAllExchangeRateProviders().FirstOrDefault();
            return exchangeRateProvider;
        }

        /// <summary>
        /// Load exchange rate provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found exchange rate provider</returns>
        public virtual IExchangeRateProvider LoadExchangeRateProviderBySystemName(string systemName)
        {
            return _exchangeRateProvider.FirstOrDefault(x => x.SystemName == systemName);
        }

        /// <summary>
        /// Load all exchange rate providers
        /// </summary>
        /// <returns>Exchange rate providers</returns>
        public virtual IList<IExchangeRateProvider> LoadAllExchangeRateProviders()
        {
            var exchangeRateProviders = _exchangeRateProvider.ToList();
            return exchangeRateProviders
                .OrderBy(tp => tp.Priority)
                .ToList();
        }
    }
}
