using Grand.Business.Core.Interfaces.Common.Providers;
using Grand.Domain.Directory;

namespace Grand.Business.Core.Interfaces.Common.Directory;

public interface IExchangeRateService
{
    /// <summary>
    ///     Gets currency live rates
    /// </summary>
    /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
    /// <param name="cancellationToken">Cancels the outbound request</param>
    /// <returns>Exchange rates</returns>
    Task<IList<ExchangeRate>> GetCurrencyLiveRates(string exchangeRateCurrencyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Load active exchange rate provider
    /// </summary>
    /// <returns>Active exchange rate provider</returns>
    IExchangeRateProvider LoadActiveExchangeRateProvider();

    /// <summary>
    ///     Load exchange rate provider by system name
    /// </summary>
    /// <param name="systemName">System name</param>
    /// <returns>Found exchange rate provider</returns>
    IExchangeRateProvider LoadExchangeRateProviderBySystemName(string systemName);

    /// <summary>
    ///     Load all exchange rate providers
    /// </summary>
    /// <returns>Exchange rate providers</returns>
    IList<IExchangeRateProvider> LoadAllExchangeRateProviders();
}