using Grand.Business.Common.Interfaces.Providers;
using Grand.SharedKernel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Plugin.ExchangeRate.McExchange
{
    public class McExchangeRateProvider : IExchangeRateProvider
    {

        private readonly IDictionary<string, IRateProvider> _providers = new Dictionary<string, IRateProvider> {
            {"eur", new EcbExchange()},
            {"pln", new NbpExchange()},
        };

        public McExchangeRateProvider()
        {

        }

        public string ConfigurationUrl => "";

        public string SystemName => "CurrencyExchange.MoneyConverter";

        public string FriendlyName => "Money converter exchange rate provider";

        public int Priority => 0;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        public Task<IList<Domain.Directory.ExchangeRate>> GetCurrencyLiveRates(string exchangeRateCurrencyCode)
        {
            if (string.IsNullOrEmpty(exchangeRateCurrencyCode))
                throw new ArgumentNullException(nameof(exchangeRateCurrencyCode));

            exchangeRateCurrencyCode = exchangeRateCurrencyCode.ToLowerInvariant();

            if (_providers.TryGetValue(exchangeRateCurrencyCode, out var provider))
            {
                return provider.GetCurrencyLiveRates();
            }

            throw new GrandException("You can use ECB (European central bank) exchange rate provider only when the primary exchange rate currency is set to EURO");
        }

    }
}
