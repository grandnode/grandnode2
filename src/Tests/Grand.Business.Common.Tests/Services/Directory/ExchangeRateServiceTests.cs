using Grand.Business.Common.Services.Directory;
using Grand.Business.Core.Interfaces.Common.Providers;
using Grand.Domain.Directory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Directory;

[TestClass]
public class ExchangeRateServiceTests
{
    private CurrencySettings _settings;

    [TestInitialize]
    public void Init()
    {
        _settings = new CurrencySettings();
    }

    [TestMethod]
    public async Task GetCurrencyLiveRates_InvokeExpectedMethod()
    {
        var provider = new Mock<IExchangeRateProvider>();
        provider.Setup(c => c.SystemName).Returns("sysname");
        _settings.ActiveExchangeRateProviderSystemName = "sysname";
        var exchangeRateService =
            new ExchangeRateService(new List<IExchangeRateProvider> { provider.Object }, _settings);
        await exchangeRateService.GetCurrencyLiveRates("rate");
        provider.Verify(c => c.GetCurrencyLiveRates(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    //a provider built before cancellation support implements only the overload without a token
    private sealed class ProviderWithoutCancellation : IExchangeRateProvider
    {
        public string SystemName => "sysname";
        public string FriendlyName => "legacy";
        public int Priority => 0;
        public IList<string> LimitedToStores => [];
        public IList<string> LimitedToGroups => [];
        public string ConfigurationUrl => "";

        public Task<IList<ExchangeRate>> GetCurrencyLiveRates(string exchangeRateCurrencyCode)
        {
            return Task.FromResult<IList<ExchangeRate>>([new ExchangeRate { CurrencyCode = exchangeRateCurrencyCode }]);
        }
    }

    [TestMethod]
    public async Task GetCurrencyLiveRates_ProviderWithoutCancellation_StillAnswers()
    {
        _settings.ActiveExchangeRateProviderSystemName = "sysname";
        var exchangeRateService =
            new ExchangeRateService(new List<IExchangeRateProvider> { new ProviderWithoutCancellation() }, _settings);
        using var cts = new CancellationTokenSource();

        var rates = await exchangeRateService.GetCurrencyLiveRates("EUR", cts.Token);

        Assert.AreEqual("EUR", rates.Single().CurrencyCode);
    }

    [TestMethod]
    public async Task GetCurrencyLiveRates_PassesTokenToProvider()
    {
        var provider = new Mock<IExchangeRateProvider>();
        provider.Setup(c => c.SystemName).Returns("sysname");
        _settings.ActiveExchangeRateProviderSystemName = "sysname";
        var exchangeRateService =
            new ExchangeRateService(new List<IExchangeRateProvider> { provider.Object }, _settings);
        using var cts = new CancellationTokenSource();

        await exchangeRateService.GetCurrencyLiveRates("rate", cts.Token);

        provider.Verify(c => c.GetCurrencyLiveRates("rate", cts.Token), Times.Once);
    }

    [TestMethod]
    public void LoadBySystemName_ReturnExpectedValue()
    {
        var provider = new Mock<IExchangeRateProvider>();
        provider.Setup(c => c.SystemName).Returns("sysname");
        var exchangeRateService =
            new ExchangeRateService(new List<IExchangeRateProvider> { provider.Object }, _settings);
        var result = exchangeRateService.LoadExchangeRateProviderBySystemName("sysname");
        Assert.IsNotNull(result);
        Assert.AreEqual("sysname", result.SystemName);
    }
}