using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.System.Interfaces.ScheduleTasks;
using Grand.Domain.Directory;
using Grand.Domain.Tasks;
using System;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    /// <summary>
    /// Represents a task for updating exchange rates
    /// </summary>
    public partial class UpdateExchangeRateScheduleTask : ScheduleTask, IScheduleTask
    {
        private readonly ICurrencyService _currencyService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly CurrencySettings _currencySettings;
        public UpdateExchangeRateScheduleTask(
            ICurrencyService currencyService,
            IExchangeRateService exchangeRateService,
            CurrencySettings currencySettings)
        {
            _currencyService = currencyService;
            _exchangeRateService = exchangeRateService;
            _currencySettings = currencySettings;
        }

        /// <summary>
        /// Executes a task
        /// </summary>
        public async Task Execute()
        {
            if (!_currencySettings.AutoUpdateEnabled)
                return;

            var primaryCurrencyCode = (await _currencyService.GetPrimaryExchangeRateCurrency()).CurrencyCode;
            var exchangeRates = await _exchangeRateService.GetCurrencyLiveRates(primaryCurrencyCode);

            foreach (var exchageRate in exchangeRates)
            {
                var currency = await _currencyService.GetCurrencyByCode(exchageRate.CurrencyCode);
                if (currency != null)
                {
                    currency.Rate = exchageRate.Rate;
                    currency.UpdatedOnUtc = DateTime.UtcNow;
                    await _currencyService.UpdateCurrency(currency);
                }
            }
        }
    }
}
