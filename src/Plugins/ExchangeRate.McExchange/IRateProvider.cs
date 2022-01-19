namespace ExchangeRate.McExchange
{
    internal interface IRateProvider
    {
        Task<IList<Grand.Domain.Directory.ExchangeRate>> GetCurrencyLiveRates();
    }
}
