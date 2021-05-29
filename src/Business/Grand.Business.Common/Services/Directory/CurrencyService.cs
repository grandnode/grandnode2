using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Domain.Data;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.Directory
{
    /// <summary>
    /// Currency service
    /// </summary>
    public partial class CurrencyService : ICurrencyService
    {
        #region Fields

        private readonly IRepository<Currency> _currencyRepository;
        private readonly IAclService _aclService;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;
        private readonly CurrencySettings _currencySettings;
        private Currency _primaryCurrency;
        private Currency _primaryExchangeRateCurrency;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
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

        #region Methods

        /// <summary>
        /// Gets a currency
        /// </summary>
        /// <param name="currencyId">Currency identifier</param>
        /// <returns>Currency</returns>
        public virtual Task<Currency> GetCurrencyById(string currencyId)
        {
            string key = string.Format(CacheKey.CURRENCIES_BY_ID_KEY, currencyId);
            return _cacheBase.GetAsync(key, () => _currencyRepository.GetByIdAsync(currencyId));
        }

        /// <summary>
        /// Gets primary store currency
        /// </summary>
        /// <returns>Currency</returns>
        public async Task<Currency> GetPrimaryStoreCurrency()
        {
            if (_primaryCurrency == null)
                _primaryCurrency = await GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);

            return _primaryCurrency;
        }

        /// <summary>
        /// Gets primary exchange currency
        /// </summary>
        /// <returns>Currency</returns>
        public async Task<Currency> GetPrimaryExchangeRateCurrency()
        {
            if (_primaryExchangeRateCurrency == null)
                _primaryExchangeRateCurrency = await GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);

            return _primaryExchangeRateCurrency;
        }

        /// <summary>
        /// Gets a currency by code
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
                            where q.CurrencyCode.ToLowerInvariant() == currencyCode.ToLower()
                            select q;
                return await Task.FromResult(query.FirstOrDefault());
            });
        }

        /// <summary>
        /// Gets all currencies
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass "" to load all records</param>
        /// <returns>Currencies</returns>
        public virtual async Task<IList<Currency>> GetAllCurrencies(bool showHidden = false, string storeId = "")
        {
            string key = string.Format(CacheKey.CURRENCIES_ALL_KEY, showHidden);
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
            {
                currencies = currencies.Where(c => _aclService.Authorize(c, storeId)).ToList();
            }
            return currencies;
        }

        /// <summary>
        /// Inserts a currency
        /// </summary>
        /// <param name="currency">Currency</param>
        public virtual async Task InsertCurrency(Currency currency)
        {
            if (currency == null)
                throw new ArgumentNullException(nameof(currency));

            await _currencyRepository.InsertAsync(currency);

            await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(currency);
        }

        /// <summary>
        /// Updates the currency
        /// </summary>
        /// <param name="currency">Currency</param>
        public virtual async Task UpdateCurrency(Currency currency)
        {
            if (currency == null)
                throw new ArgumentNullException(nameof(currency));

            await _currencyRepository.UpdateAsync(currency);

            await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(currency);
        }

        /// <summary>
        /// Deletes currency
        /// </summary>
        /// <param name="currency">Currency</param>
        public virtual async Task DeleteCurrency(Currency currency)
        {
            if (currency == null)
                throw new ArgumentNullException(nameof(currency));

            await _currencyRepository.DeleteAsync(currency);

            await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(currency);
        }


        /// <summary>
        /// Converts currency
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
        /// Converts currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrencyCode">Source currency code</param>
        /// <param name="targetCurrencyCode">Target currency code</param>
        /// <returns>Converted value</returns>
        public virtual async Task<double> ConvertCurrency(double amount, Currency sourceCurrencyCode, Currency targetCurrencyCode)
        {
            if (sourceCurrencyCode == null)
                throw new ArgumentNullException(nameof(sourceCurrencyCode));

            if (targetCurrencyCode == null)
                throw new ArgumentNullException(nameof(targetCurrencyCode));

            var result = amount;

            if (result == 0 || sourceCurrencyCode.Id == targetCurrencyCode.Id)
                return result;

            result = await ConvertToPrimaryExchangeRateCurrency(result, sourceCurrencyCode);
            result = await ConvertFromPrimaryExchangeRateCurrency(result, targetCurrencyCode);

            return result;

        }

        /// <summary>
        /// Converts to primary exchange rate currency 
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrencyCode">Source currency code</param>
        /// <returns>Converted value</returns>
        public virtual async Task<double> ConvertToPrimaryExchangeRateCurrency(double amount, Currency sourceCurrencyCode)
        {
            if (sourceCurrencyCode == null)
                throw new ArgumentNullException(nameof(sourceCurrencyCode));

            var primaryExchangeRateCurrency = await GetPrimaryExchangeRateCurrency();
            if (primaryExchangeRateCurrency == null)
                throw new Exception("Primary exchange rate currency cannot be loaded");

            double result = amount;
            double exchangeRate = sourceCurrencyCode.Rate;
            if (exchangeRate == 0)
                throw new GrandException(string.Format("Exchange rate not found for currency [{0}]", sourceCurrencyCode.Name));
            result = result / exchangeRate;
            return result;
        }

        /// <summary>
        /// Converts from primary exchange rate currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="targetCurrencyCode">Target currency code</param>
        /// <returns>Converted value</returns>
        public virtual async Task<double> ConvertFromPrimaryExchangeRateCurrency(double amount, Currency targetCurrencyCode)
        {
            if (targetCurrencyCode == null)
                throw new ArgumentNullException(nameof(targetCurrencyCode));

            var primaryExchangeRateCurrency = await GetPrimaryExchangeRateCurrency();
            if (primaryExchangeRateCurrency == null)
                throw new Exception("Primary exchange rate currency cannot be loaded");

            double result = amount;

            double exchangeRate = targetCurrencyCode.Rate;
            if (exchangeRate == 0)
                throw new GrandException(string.Format("Exchange rate not found for currency [{0}]", targetCurrencyCode.Name));

            result = result * exchangeRate;

            return result;
        }

        /// <summary>
        /// Converts to primary store currency 
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrencyCode">Source currency code</param>
        /// <returns>Converted value</returns>
        public virtual async Task<double> ConvertToPrimaryStoreCurrency(double amount, Currency sourceCurrencyCode)
        {
            if (sourceCurrencyCode == null)
                throw new ArgumentNullException(nameof(sourceCurrencyCode));

            var primaryStoreCurrency = await GetPrimaryStoreCurrency();
            var result = await ConvertCurrency(amount, sourceCurrencyCode, primaryStoreCurrency);

            return result;
        }

        /// <summary>
        /// Converts from primary store currency
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
}