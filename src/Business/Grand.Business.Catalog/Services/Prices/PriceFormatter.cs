using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.SharedKernel;
using System.Globalization;

namespace Grand.Business.Catalog.Services.Prices
{
    /// <summary>
    /// Price formatter
    /// </summary>
    public class PriceFormatter : IPriceFormatter
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly ITranslationService _translationService;
        private readonly TaxSettings _taxSettings;

        #endregion

        #region Constructors

        public PriceFormatter(IWorkContext workContext,
            ICurrencyService currencyService,
            ITranslationService translationService,
            TaxSettings taxSettings)
        {
            _workContext = workContext;
            _currencyService = currencyService;
            _translationService = translationService;
            _taxSettings = taxSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets currency string
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <returns>Currency string without exchange rate</returns>
        protected virtual string GetCurrencyString(double amount, Currency targetCurrency)
        {
            if (targetCurrency == null)
                return amount.ToString("C");

            var result = "";
            if (!string.IsNullOrEmpty(targetCurrency.CustomFormatting))
            {
                var cultureInfo = !string.IsNullOrEmpty(targetCurrency.DisplayLocale) ? new CultureInfo(targetCurrency.DisplayLocale) : null;
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
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        public virtual string FormatPrice(double price)
        {
            return FormatPrice(price, _workContext.WorkingCurrency);
        }

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <returns>Price</returns>
        public virtual string FormatPrice(double price, Currency targetCurrency)
        {
            var priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            return FormatPrice(price, targetCurrency, _workContext.WorkingLanguage, priceIncludesTax);
        }

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="showTax">Indicates if it should show tax suffix</param>
        /// <returns>Price</returns>
        public virtual string FormatPrice(double price, bool showTax)
        {
            var priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            return FormatPrice(price, _workContext.WorkingCurrency, _workContext.WorkingLanguage, priceIncludesTax, showTax);
        }

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="showTax">A value indicating whether to show tax suffix</param>
        /// <param name="language">Language</param>
        /// <returns>Price</returns>
        public virtual async Task<string> FormatPrice(double price, string currencyCode, bool showTax, Language language)
        {
            var currency = !string.IsNullOrEmpty(currencyCode) ? await _currencyService.GetCurrencyByCode(currencyCode) : null;
            var priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            return FormatPrice(price, currency, language, priceIncludesTax, showTax);
        }

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <returns>Price</returns>
        public virtual async Task<string> FormatPrice(double price, string currencyCode, Language language, bool priceIncludesTax)
        {
            var currency = !string.IsNullOrEmpty(currencyCode) ? await _currencyService.GetCurrencyByCode(currencyCode) : null;
            return FormatPrice(price, currency, language, priceIncludesTax);
        }

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <returns>Price</returns>
        public virtual string FormatPrice(double price, Currency targetCurrency, Language language, bool priceIncludesTax)
        {
            return FormatPrice(price, targetCurrency, language, priceIncludesTax, _taxSettings.DisplayTaxSuffix);
        }

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">Indicates if price includes tax</param>
        /// <param name="showTax">Indicates if it should show tax suffix</param>
        /// <returns>Price</returns>
        public string FormatPrice(double price, Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax)
        {
            var currencyString = GetCurrencyString(price, targetCurrency);
            if (!showTax) return currencyString;
            //show tax suffix
            string formatStr;
            if (priceIncludesTax)
            {
                formatStr = _translationService.GetResource("Products.InclTaxSuffix", language.Id);
                if (string.IsNullOrEmpty(formatStr))
                    formatStr = "{0} incl tax";
            }
            else
            {
                formatStr = _translationService.GetResource("Products.ExclTaxSuffix", language.Id);
                if (string.IsNullOrEmpty(formatStr))
                    formatStr = "{0} excl tax";
            }
            return string.Format(formatStr, currencyString);

        }



        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        public virtual string FormatShippingPrice(double price)
        {
            var priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            return FormatShippingPrice(price, _workContext.WorkingCurrency, _workContext.WorkingLanguage, priceIncludesTax);
        }

        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">Indicates if price includes tax</param>
        /// <returns>Price</returns>
        public virtual string FormatShippingPrice(double price, Currency targetCurrency, Language language, bool priceIncludesTax)
        {
            var showTax = _taxSettings.ShippingIsTaxable && _taxSettings.DisplayTaxSuffix;
            return FormatShippingPrice(price, targetCurrency, language, priceIncludesTax, showTax);
        }

        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">Indicates if price includes tax</param>
        /// <param name="showTax">Indicates if it should show a tax suffix</param>
        /// <returns>Price</returns>
        public virtual string FormatShippingPrice(double price,
            Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax)
        {
            return FormatPrice(price, targetCurrency, language, priceIncludesTax, showTax);
        }

        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">Indicates if price includes tax</param>
        /// <returns>Price</returns>
        public virtual async Task<string> FormatShippingPrice(double price, string currencyCode, Language language, bool priceIncludesTax)
        {
            var currency = !string.IsNullOrEmpty(currencyCode) ? await _currencyService.GetCurrencyByCode(currencyCode) : null;
            return FormatShippingPrice(price, currency, language, priceIncludesTax);
        }

        /// <summary>
        /// Resource name of rental product (rental period)
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>Rental product price with period</returns>
        public virtual string ResourceReservationProductPeriod(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (product.ProductTypeId != ProductType.Reservation)
                return string.Empty;

            var result = product.IntervalUnitId switch {
                IntervalUnit.Day => "Products.Price.Reservation.Days",
                IntervalUnit.Hour => "Products.Price.Reservation.Hour",
                IntervalUnit.Minute => "Products.Price.Reservation.Minute",
                _ => throw new GrandException("Not supported reservation period")
            };

            return result;
        }


        /// <summary>
        /// Payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        public virtual string FormatPaymentMethodAdditionalFee(double price)
        {
            var priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
            return FormatPaymentMethodAdditionalFee(price, _workContext.WorkingCurrency,
                _workContext.WorkingLanguage, priceIncludesTax);
        }

        /// <summary>
        /// Formats the payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">Indicates if price includes tax</param>
        /// <returns>Price</returns>
        public virtual string FormatPaymentMethodAdditionalFee(double price,
            Currency targetCurrency, Language language, bool priceIncludesTax)
        {
            var showTax = _taxSettings.PaymentMethodAdditionalFeeIsTaxable && _taxSettings.DisplayTaxSuffix;
            return FormatPaymentMethodAdditionalFee(price, targetCurrency, language, priceIncludesTax, showTax);
        }

        /// <summary>
        /// Formats the payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">Indicates if price includes tax</param>
        /// <param name="showTax">Indicates if it should show a tax suffix</param>
        /// <returns>Price</returns>
        public virtual string FormatPaymentMethodAdditionalFee(double price,
            Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax)
        {
            return FormatPrice(price, targetCurrency, language, priceIncludesTax, showTax);
        }

        /// <summary>
        /// Formats the payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">Indicates if price includes tax</param>
        /// <returns>Price</returns>
        public virtual async Task<string> FormatPaymentMethodAdditionalFee(double price,
            string currencyCode, Language language, bool priceIncludesTax)
        {
            var currency = !string.IsNullOrEmpty(currencyCode) ? await _currencyService.GetCurrencyByCode(currencyCode) : null;
            return FormatPaymentMethodAdditionalFee(price, currency,
                language, priceIncludesTax);
        }

        /// <summary>
        /// Formats a tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        /// <returns>Formatted tax rate</returns>
        public virtual string FormatTaxRate(double taxRate)
        {
            return taxRate.ToString("G29");
        }

        #endregion
    }
}
