using Grand.Business.Core.Interfaces.Common.Providers;
using Grand.SharedKernel;
using System.Net.Http;

namespace ExchangeRate.McExchange
{
    public class McExchangeRateProvider : IExchangeRateProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly IDictionary<string, IRateProvider> _providers;

        public McExchangeRateProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

            _providers = new Dictionary<string, IRateProvider> {
                {"eur", new EcbExchange(_httpClientFactory)},
                {"pln", new NbpExchange(_httpClientFactory)}
            };
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
        public Task<IList<Grand.Domain.Directory.ExchangeRate>> GetCurrencyLiveRates(string exchangeRateCurrencyCode)
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
