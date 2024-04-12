using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Domain.Directory;
using Grand.Infrastructure;
using System.Globalization;

namespace Grand.Business.Catalog.Services.Prices;

/// <summary>
///     Price formatter
/// </summary>
public class PriceFormatter : IPriceFormatter
{
    #region Fields

    private readonly IWorkContext _workContext;

    #endregion

    #region Constructors

    public PriceFormatter(IWorkContext workContext)
    {
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>
    ///     Gets currency string
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <param name="targetCurrency">Target currency</param>
    /// <returns>Currency string without exchange rate</returns>
    protected virtual string GetCurrencyString(double amount, Currency targetCurrency)
    {
        if (targetCurrency == null)
            return amount.ToString("C");

        string result;
        if (!string.IsNullOrEmpty(targetCurrency.CustomFormatting))
        {
            var cultureInfo = !string.IsNullOrEmpty(targetCurrency.DisplayLocale)
                ? new CultureInfo(targetCurrency.DisplayLocale)
                : null;
            result = amount.ToString(targetCurrency.CustomFormatting, cultureInfo);
        }
        else
        {
            if (!string.IsNullOrEmpty(targetCurrency.DisplayLocale))
            {
                result = amount.ToString("C", new CultureInfo(targetCurrency.DisplayLocale));
            }
            else
            {
                result = $"{amount:N} ({targetCurrency.CurrencyCode})";
                return result;
            }
        }

        return result;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Formats the price
    /// </summary>
    /// <param name="price">Price</param>
    /// <returns>Price</returns>
    public virtual string FormatPrice(double price)
    {
        return FormatPrice(price, _workContext.WorkingCurrency);
    }

    /// <summary>
    ///     Formats the price
    /// </summary>
    /// <param name="price">Price</param>
    /// <param name="targetCurrency">Target currency</param>
    /// <returns>Price</returns>
    public string FormatPrice(double price, Currency targetCurrency)
    {
        var currencyString = GetCurrencyString(price, targetCurrency);
        return currencyString;
    }

    /// <summary>
    ///     Formats a tax rate
    /// </summary>
    /// <param name="taxRate">Tax rate</param>
    /// <returns>Formatted tax rate</returns>
    public virtual string FormatTaxRate(double taxRate)
    {
        return taxRate.ToString("G29");
    }

    #endregion
}