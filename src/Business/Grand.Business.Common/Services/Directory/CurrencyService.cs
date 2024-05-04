using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;
using MediatR;

namespace Grand.Business.Common.Services.Directory;

/// <summary>
///     Currency service
/// </summary>
public class CurrencyService : ICurrencyService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="currencyRepository">Currency repository</param>
    /// <param name="aclService">ACL service</param>
    /// <param name="currencySettings">Currency settings</param>
    /// <param name="mediator">Mediator</param>
    public CurrencyService(ICacheBase cacheBase,
        IRepository<Currency> currencyRepository,
        IAclService aclService,
        CurrencySettings currencySettings,
        IMediator mediator)
    {
        _cacheBase = cacheBase;
        _currencyRepository = currencyRepository;
        _aclService = aclService;
        _currencySettings = currencySettings;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<Currency> _currencyRepository;
    private readonly IAclService _aclService;
    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;
    private readonly CurrencySettings _currencySettings;
    private Currency _primaryCurrency;
    private Currency _primaryExchangeRateCurrency;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a currency
    /// </summary>
    /// <param name="currencyId">Currency identifier</param>
    /// <returns>Currency</returns>
    public virtual Task<Currency> GetCurrencyById(string currencyId)
    {
        var key = string.Format(CacheKey.CURRENCIES_BY_ID_KEY, currencyId);
        return _cacheBase.GetAsync(key, () => _currencyRepository.GetByIdAsync(currencyId));
    }

    /// <summary>
    ///     Gets primary store currency
    /// </summary>
    /// <returns>Currency</returns>
    public async Task<Currency> GetPrimaryStoreCurrency()
    {
        return _primaryCurrency ??= await GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
    }

    /// <summary>
    ///     Gets primary exchange currency
    /// </summary>
    /// <returns>Currency</returns>
    public async Task<Currency> GetPrimaryExchangeRateCurrency()
    {
        return _primaryExchangeRateCurrency ??= await GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);
    }

    /// <summary>
    ///     Gets a currency by code
    /// </summary>
    /// <param name="currencyCode">Currency code</param>
    /// <returns>Currency</returns>
    public virtual async Task<Currency> GetCurrencyByCode(string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return null;

        var key = string.Format(CacheKey.CURRENCIES_BY_CODE, currencyCode);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from q in _currencyRepository.Table
                where q.CurrencyCode.ToLowerInvariant() == currencyCode.ToLowerInvariant()
                select q;
            return await Task.FromResult(query.FirstOrDefault());
        });
    }

    /// <summary>
    ///     Gets all currencies
    /// </summary>
    /// <param name="showHidden">A value indicating whether to show hidden records</param>
    /// <param name="storeId">Load records allowed only in a specified store; pass "" to load all records</param>
    /// <returns>Currencies</returns>
    public virtual async Task<IList<Currency>> GetAllCurrencies(bool showHidden = false, string storeId = "")
    {
        var key = string.Format(CacheKey.CURRENCIES_ALL_KEY, showHidden);
        var currencies = await _cacheBase.GetAsync(key, async () =>
        {
            var query = from p in _currencyRepository.Table
                select p;

            if (!showHidden)
                query = query.Where(c => c.Published);
            query = query.OrderBy(c => c.DisplayOrder);
            return await Task.FromResult(query.ToList());
        });

        //store acl
        if (!string.IsNullOrEmpty(storeId))
            currencies = currencies.Where(c => _aclService.Authorize(c, storeId)).ToList();
        return currencies;
    }

    /// <summary>
    ///     Inserts a currency
    /// </summary>
    /// <param name="currency">Currency</param>
    public virtual async Task InsertCurrency(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        await _currencyRepository.InsertAsync(currency);

        await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(currency);
    }

    /// <summary>
    ///     Updates the currency
    /// </summary>
    /// <param name="currency">Currency</param>
    public virtual async Task UpdateCurrency(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        await _currencyRepository.UpdateAsync(currency);

        await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(currency);
    }

    /// <summary>
    ///     Deletes currency
    /// </summary>
    /// <param name="currency">Currency</param>
    public virtual async Task DeleteCurrency(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        await _currencyRepository.DeleteAsync(currency);

        await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(currency);
    }


    /// <summary>
    ///     Converts currency
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <param name="exchangeRate">Currency exchange rate</param>
    /// <returns>Converted value</returns>
    public virtual double ConvertCurrency(double amount, double exchangeRate)
    {
        if (amount != 0 && exchangeRate != 0)
            return Math.Round(amount * exchangeRate, 6);
        return 0;
    }

    /// <summary>
    ///     Converts currency
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <param name="sourceCurrencyCode">Source currency code</param>
    /// <param name="targetCurrencyCode">Target currency code</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertCurrency(double amount, Currency sourceCurrencyCode,
        Currency targetCurrencyCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCurrencyCode);
        ArgumentNullException.ThrowIfNull(targetCurrencyCode);

        var result = amount;

        if (result == 0 || sourceCurrencyCode.Id == targetCurrencyCode.Id)
            return result;

        result = await ConvertToPrimaryExchangeRateCurrency(result, sourceCurrencyCode);
        result = await ConvertFromPrimaryExchangeRateCurrency(result, targetCurrencyCode);

        return result;
    }

    /// <summary>
    ///     Converts to primary exchange rate currency
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <param name="sourceCurrencyCode">Source currency code</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertToPrimaryExchangeRateCurrency(double amount, Currency sourceCurrencyCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCurrencyCode);

        var primaryExchangeRateCurrency = await GetPrimaryExchangeRateCurrency();
        if (primaryExchangeRateCurrency == null)
            throw new Exception("Primary exchange rate currency cannot be loaded");

        var result = amount;
        var exchangeRate = sourceCurrencyCode.Rate;
        if (exchangeRate == 0)
            throw new GrandException($"Exchange rate not found for currency [{sourceCurrencyCode.Name}]");
        result /= exchangeRate;
        return result;
    }

    /// <summary>
    ///     Converts from primary exchange rate currency
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <param name="targetCurrencyCode">Target currency code</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertFromPrimaryExchangeRateCurrency(double amount, Currency targetCurrencyCode)
    {
        ArgumentNullException.ThrowIfNull(targetCurrencyCode);

        var primaryExchangeRateCurrency = await GetPrimaryExchangeRateCurrency();
        if (primaryExchangeRateCurrency == null)
            throw new Exception("Primary exchange rate currency cannot be loaded");

        var result = amount;

        var exchangeRate = targetCurrencyCode.Rate;
        if (exchangeRate == 0)
            throw new GrandException($"Exchange rate not found for currency [{targetCurrencyCode.Name}]");

        result *= exchangeRate;

        return result;
    }

    /// <summary>
    ///     Converts to primary store currency
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <param name="sourceCurrencyCode">Source currency code</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertToPrimaryStoreCurrency(double amount, Currency sourceCurrencyCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCurrencyCode);

        var primaryStoreCurrency = await GetPrimaryStoreCurrency();
        var result = await ConvertCurrency(amount, sourceCurrencyCode, primaryStoreCurrency);

        return result;
    }

    /// <summary>
    ///     Converts from primary store currency
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <param name="targetCurrencyCode">Target currency code</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertFromPrimaryStoreCurrency(double amount, Currency targetCurrencyCode)
    {
        if (targetCurrencyCode == null)
            return amount;

        var result = await ConvertCurrency(amount, await GetPrimaryStoreCurrency(), targetCurrencyCode);
        return result;
    }

    #endregion
}