using Grand.Business.Common.Interfaces.Providers;
using Grand.Business.Common.Services.Directory;
using Grand.Domain.Directory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Directory
{
    [TestClass()]
    public class ExchangeRateServiceTests
    {
        private CurrencySettings _settings;

        [TestInitialize()]
        public void Init()
        {
            _settings = new CurrencySettings();
        }

        [TestMethod()]
        public async Task GetCurrencyLiveRates_InvokeExpectedMethod()
        {
            var provider = new Mock<IExchangeRateProvider>();
            provider.Setup(c => c.SystemName).Returns("sysname");
            _settings.ActiveExchangeRateProviderSystemName = "sysname";
            var exchangeRateService = new ExchangeRateService(new List<IExchangeRateProvider>() { provider.Object},_settings);
            await exchangeRateService.GetCurrencyLiveRates("rate");
            provider.Verify(c => c.GetCurrencyLiveRates(It.IsAny<string>()), Times.Once);
        }

        [TestMethod()]
        public void LoadBySystemName_ReturnExpectedValue()
        {
            var provider = new Mock<IExchangeRateProvider>();
            provider.Setup(c => c.SystemName).Returns("sysname");
            var exchangeRateService = new ExchangeRateService(new List<IExchangeRateProvider>() { provider.Object }, _settings);
            var result =exchangeRateService.LoadExchangeRateProviderBySystemName("sysname");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.SystemName, "sysname");
        }
    }
}
