using Grand.Business.Common.Interfaces.Providers;
using Grand.Domain.Directory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Directory
{
    public interface IExchangeRateService
    {
        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        Task<IList<ExchangeRate>> GetCurrencyLiveRates(string exchangeRateCurrencyCode);

        /// <summary>
        /// Load active exchange rate provider
        /// </summary>
        /// <returns>Active exchange rate provider</returns>
        IExchangeRateProvider LoadActiveExchangeRateProvider();

        /// <summary>
        /// Load exchange rate provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found exchange rate provider</returns>
        IExchangeRateProvider LoadExchangeRateProviderBySystemName(string systemName);

        /// <summary>
        /// Load all exchange rate providers
        /// </summary>
        /// <returns>Exchange rate providers</returns>
        IList<IExchangeRateProvider> LoadAllExchangeRateProviders();
    }
}
