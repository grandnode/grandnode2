using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Mediator;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Services;

public class CurrencyViewModelService : ICurrencyViewModelService
{
    #region Fields

    private readonly ICurrencyService _currencyService;
    private readonly IStoreService _storeService;
    private readonly CurrencySettings _currencySettings;
    private readonly ISettingService _settingService;
    private readonly IRequestTranslationService _translationService;
    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;

    #endregion

    #region Constructors

    public CurrencyViewModelService(ICurrencyService currencyService,
        IStoreService storeService,
        CurrencySettings currencySettings,
        ISettingService settingService,
        IRequestTranslationService translationService,
        ICacheBase cacheBase,
        IMediator mediator)
    {
        _currencyService = currencyService;
        _storeService = storeService;
        _currencySettings = currencySettings;
        _settingService = settingService;
        _translationService = translationService;
        _cacheBase = cacheBase;
        _mediator = mediator;
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

    public virtual async Task<(bool canProceed, string message)> ValidateCurrencyStoreMapping(Currency currency,
        CurrencyModel model)
    {
        var limitedToStores = model.Stores is { Length: > 0 };

        //a published currency available to every store can never leave a store without one
        if (model.Published && !limitedToStores)
            return (true, string.Empty);

        var otherCurrencies = (await _currencyService.GetAllCurrencies())
            .Where(c => c.Id != currency.Id).ToList();

        foreach (var store in await _storeService.GetAllStores())
        {
            if (otherCurrencies.Any(c => !c.LimitedToStores || c.Stores.Contains(store.Id)))
                continue;

            if (model.Published && (!limitedToStores || model.Stores.Contains(store.Id)))
                continue;

            return (false, string.Format(
                _translationService.GetResource("Admin.Configuration.Currencies.CantLimitStores"), store.Name));
        }

        return (true, string.Empty);
    }

    public virtual async Task<(bool canDelete, string message)> ValidateCurrencyDelete(Currency currency)
    {
        if (currency.Id == _currencySettings.PrimaryStoreCurrencyId)
            return (false, _translationService.GetResource("Admin.Configuration.Currencies.CantDeletePrimary"));

        if (currency.Id == _currencySettings.PrimaryExchangeRateCurrencyId)
            return (false, _translationService.GetResource("Admin.Configuration.Currencies.CantDeleteExchange"));

        //an order keeps the code of the currency it was placed in: its pages, refunds and loyalty points need it
        if (!string.IsNullOrEmpty(currency.CurrencyCode) &&
            (await _mediator.Send(new GetOrderQuery { CustomerCurrencyCode = currency.CurrencyCode, PageSize = 1 })).TotalCount > 0)
            return (false, _translationService.GetResource("Admin.Configuration.Currencies.CantDeleteUsedByOrders"));

        var allCurrencies = await _currencyService.GetAllCurrencies();
        if (allCurrencies.Count == 1 && allCurrencies[0].Id == currency.Id)
            return (false, "At least one published currency is required.");

        return (true, string.Empty);
    }
}