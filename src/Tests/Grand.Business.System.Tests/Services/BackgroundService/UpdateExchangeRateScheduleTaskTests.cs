using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Grand.Domain.Directory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.System.Tests.Services.BackgroundService
{
    [TestClass]
    public class UpdateExchangeRateScheduleTaskTests
    {
        private Mock<ICurrencyService> _currencyServiceMock;
        private Mock<IExchangeRateService> _exchangeRateServiceMock;
        private CurrencySettings _settings;
        private UpdateExchangeRateScheduleTask _task;

        [TestInitialize]
        public void Init()
        {
            _currencyServiceMock = new Mock<ICurrencyService>();
            _exchangeRateServiceMock = new Mock<IExchangeRateService>();
            _settings = new CurrencySettings();
            _task = new UpdateExchangeRateScheduleTask(_currencyServiceMock.Object,_exchangeRateServiceMock.Object,_settings);
        }

        [TestMethod]
        public async Task Execute_InvokeExpectedMethods()
        {
            _settings.AutoUpdateEnabled = true;
            _currencyServiceMock.Setup(c => c.GetPrimaryExchangeRateCurrency()).ReturnsAsync(new Currency() { CurrencyCode = "EU" });
            _exchangeRateServiceMock.Setup(c => c.GetCurrencyLiveRates(It.IsAny<string>()))
                .ReturnsAsync(new List<ExchangeRate>() { new ExchangeRate() { CurrencyCode="PL",Rate=10} });
            _currencyServiceMock.Setup(c => c.GetCurrencyByCode(It.IsAny<string>())).ReturnsAsync(new Currency() { Rate=10});
            await _task.Execute();

            _currencyServiceMock.Verify(c => c.UpdateCurrency(It.IsAny<Currency>()), Times.Once);
        }

        [TestMethod]
        public async Task Execute_AutoUpdateNotEnable_NotInvokeMethods()
        {
            _settings.AutoUpdateEnabled = false;
            await _task.Execute();
            _currencyServiceMock.Verify(c => c.GetPrimaryExchangeRateCurrency(), Times.Never);
            _exchangeRateServiceMock.Verify(c => c.GetCurrencyLiveRates(It.IsAny<string>()), Times.Never);
            _currencyServiceMock.Verify(c => c.UpdateCurrency(It.IsAny<Currency>()), Times.Never);
        }
    }
}
