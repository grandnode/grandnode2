using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Directory;
using Grand.Domain.Tasks;
using Grand.SharedKernel;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     Represents a task for updating exchange rates
/// </summary>
public class UpdateExchangeRateScheduleTask : ScheduleTask, IScheduleTask
{
    private readonly ICurrencyService _currencyService;
    private readonly CurrencySettings _currencySettings;
    private readonly IExchangeRateService _exchangeRateService;

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
    ///     Executes a task
    /// </summary>
    public async Task Execute(CancellationToken cancellationToken)
    {
        if (!_currencySettings.AutoUpdateEnabled)
            return;

        var primaryCurrency = await _currencyService.GetPrimaryExchangeRateCurrency() ??
                              throw new GrandException("The primary exchange rate currency is not set");
        var primaryCurrencyCode = primaryCurrency.CurrencyCode;
        var exchangeRates = await _exchangeRateService.GetCurrencyLiveRates(primaryCurrencyCode, cancellationToken);

        foreach (var exchangeRate in exchangeRates)
        {
            var currency = await _currencyService.GetCurrencyByCode(exchangeRate.CurrencyCode);
            if (currency == null) continue;
            currency.Rate = exchangeRate.Rate;
            await _currencyService.UpdateCurrency(currency);
        }
    }
}