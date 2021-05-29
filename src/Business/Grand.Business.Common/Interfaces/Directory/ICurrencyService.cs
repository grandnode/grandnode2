using Grand.Business.Common.Interfaces.Providers;
using Grand.Domain.Directory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Directory
{
    /// <summary>
    /// Currency service
    /// </summary>
    public partial interface ICurrencyService
    {
        /// <summary>
        /// Gets a currency
        /// </summary>
        /// <param name="currencyId">Currency identifier</param>
        /// <returns>Currency</returns>
        Task<Currency> GetCurrencyById(string currencyId);

        /// <summary>
        /// Gets primary store currency
        /// </summary>
        /// <returns>Currency</returns>
        Task<Currency> GetPrimaryStoreCurrency();

        /// <summary>
        /// Gets primary exchange currency
        /// </summary>
        /// <returns>Currency</returns>
        Task<Currency> GetPrimaryExchangeRateCurrency();

        /// <summary>
        /// Gets a currency by code
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <returns>Currency</returns>
        Task<Currency> GetCurrencyByCode(string currencyCode);

        /// <summary>
        /// Gets all currencies
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass "" to load all records</param>
        /// <returns>Currencies</returns>
        Task<IList<Currency>> GetAllCurrencies(bool showHidden = false, string storeId = "");

        /// <summary>
        /// Inserts a currency
        /// </summary>
        /// <param name="currency">Currency</param>
        Task InsertCurrency(Currency currency);

        /// <summary>
        /// Updates the currency
        /// </summary>
        /// <param name="currency">Currency</param>
        Task UpdateCurrency(Currency currency);

        /// <summary>
        /// Deletes currency
        /// </summary>
        /// <param name="currency">Currency</param>
        Task DeleteCurrency(Currency currency);

        /// <summary>
        /// Converts currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="exchangeRate">Currency exchange rate</param>
        /// <returns>Converted value</returns>
        double ConvertCurrency(double amount, double exchangeRate);

        /// <summary>
        /// Converts currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrencyCode">Source currency code</param>
        /// <param name="targetCurrencyCode">Target currency code</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertCurrency(double amount, Currency sourceCurrencyCode, Currency targetCurrencyCode);

        /// <summary>
        /// Converts to primary exchange rate currency 
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrencyCode">Source currency code</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertToPrimaryExchangeRateCurrency(double amount, Currency sourceCurrencyCode);

        /// <summary>
        /// Converts from primary exchange rate currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="targetCurrencyCode">Target currency code</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertFromPrimaryExchangeRateCurrency(double amount, Currency targetCurrencyCode);

        /// <summary>
        /// Converts to primary store currency 
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="sourceCurrencyCode">Source currency code</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertToPrimaryStoreCurrency(double amount, Currency sourceCurrencyCode);

        /// <summary>
        /// Converts from primary store currency
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="targetCurrencyCode">Target currency code</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertFromPrimaryStoreCurrency(double amount, Currency targetCurrencyCode);
    }
}