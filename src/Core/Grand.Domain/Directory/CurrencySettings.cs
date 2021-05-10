using Grand.Domain.Configuration;

namespace Grand.Domain.Directory
{
    public class CurrencySettings : ISettings
    {
        public string PrimaryStoreCurrencyId { get; set; }
        public string PrimaryExchangeRateCurrencyId { get; set; }
        public string ActiveExchangeRateProviderSystemName { get; set; }
        public bool AutoUpdateEnabled { get; set; }
    }
}