using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Services;

public class CurrencyViewModelService : ICurrencyViewModelService
{
    #region Fields

    private readonly ICurrencyService _currencyService;
    private readonly CurrencySettings _currencySettings;
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Constructors

    public CurrencyViewModelService(ICurrencyService currencyService,
        CurrencySettings currencySettings,
        ISettingService settingService,
        ITranslationService translationService,
        ICacheBase cacheBase)
    {
        _currencyService = currencyService;
        _currencySettings = currencySettings;
        _settingService = settingService;
        _translationService = translationService;
        _cacheBase = cacheBase;
    }

    #endregion

    public virtual CurrencyModel PrepareCurrencyModel()
    {
        var model = new CurrencyModel {
            //default values
            Published = true,
            Rate = 1
        };
        return model;
    }

    public virtual async Task<Currency> InsertCurrencyModel(CurrencyModel model)
    {
        var currency = model.ToEntity();
        await _currencyService.InsertCurrency(currency);

        return currency;
    }

    public virtual async Task<Currency> UpdateCurrencyModel(Currency currency, CurrencyModel model)
    {
        currency = model.ToEntity(currency);
        await _currencyService.UpdateCurrency(currency);
        return currency;
    }

    public virtual async Task MarkAsPrimaryExchangeRateCurrency(string id)
    {
        _currencySettings.PrimaryExchangeRateCurrencyId = id;
        await _settingService.SaveSetting(_currencySettings);
        await _cacheBase.Clear();
    }

    public virtual async Task MarkAsPrimaryStoreCurrency(string id)
    {
        _currencySettings.PrimaryStoreCurrencyId = id;
        await _settingService.SaveSetting(_currencySettings);
        await _cacheBase.Clear();
    }

    public virtual async Task<(bool canProceed, string message)> ValidateCurrencyUnpublish(string currencyId,
        bool published)
    {
        if (published)
            return (true, string.Empty);

        var allCurrencies = await _currencyService.GetAllCurrencies();
        if (allCurrencies.Count == 1 && allCurrencies[0].Id == currencyId)
            return (false, "At least one published currency is required.");

        return (true, string.Empty);
    }

    public virtual async Task<(bool canDelete, string message)> ValidateCurrencyDelete(Currency currency)
    {
        if (currency.Id == _currencySettings.PrimaryStoreCurrencyId)
            return (false, _translationService.GetResource("Admin.Configuration.Currencies.CantDeletePrimary"));

        if (currency.Id == _currencySettings.PrimaryExchangeRateCurrencyId)
            return (false, _translationService.GetResource("Admin.Configuration.Currencies.CantDeleteExchange"));

        var allCurrencies = await _currencyService.GetAllCurrencies();
        if (allCurrencies.Count == 1 && allCurrencies[0].Id == currency.Id)
            return (false, "At least one published currency is required.");

        return (true, string.Empty);
    }
}