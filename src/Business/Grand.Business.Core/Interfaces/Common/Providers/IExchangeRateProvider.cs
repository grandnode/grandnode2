using Grand.Domain.Directory;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Common.Providers;

/// <summary>
///     Exchange rate provider interface
/// </summary>
public interface IExchangeRateProvider : IProvider
{
    /// <summary>
    ///     Gets currency live rates
    /// </summary>
    /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
    /// <param name="cancellationToken">Cancels the outbound request</param>
    /// <returns>Exchange rates</returns>
    Task<IList<ExchangeRate>> GetCurrencyLiveRates(string exchangeRateCurrencyCode,
        CancellationToken cancellationToken = default);
}