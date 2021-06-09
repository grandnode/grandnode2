using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.SharedKernel;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Prices
{
    /// <summary>
    /// Price formatter
    /// </summary>
    public partial class PriceFormatter : IPriceFormatter
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

            string result = "";
            if (!String.IsNullOrEmpty(targetCurrency.CustomFormatting))
            {
                result = amount.ToString(targetCurrency.CustomFormatting);
            }
            else
            {
                if (!String.IsNullOrEmpty(targetCurrency.DisplayLocale))
                {
                    result = amount.ToString("C", new CultureInfo(targetCurrency.DisplayLocale));
                }
                else
                {
                    result = String.Format("{0} ({1})", amount.ToString("N"), targetCurrency.CurrencyCode);
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
            bool priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
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
            bool priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
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
            bool priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
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
            string currencyString = GetCurrencyString(price, targetCurrency);
            if (showTax)
            {
                //show tax suffix
                string formatStr;
                if (priceIncludesTax)
                {
                    formatStr = _translationService.GetResource("Products.InclTaxSuffix", language.Id);
                    if (String.IsNullOrEmpty(formatStr))
                        formatStr = "{0} incl tax";
                }
                else
                {
                    formatStr = _translationService.GetResource("Products.ExclTaxSuffix", language.Id);
                    if (String.IsNullOrEmpty(formatStr))
                        formatStr = "{0} excl tax";
                }
                return string.Format(formatStr, currencyString);
            }

            return currencyString;
        }



        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        public virtual string FormatShippingPrice(double price)
        {
            bool priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
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
            bool showTax = _taxSettings.ShippingIsTaxable && _taxSettings.DisplayTaxSuffix;
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
        /// Formats the price of rental product (with rental period)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="price">Price</param>
        /// <returns>Rental product price with period</returns>
        public virtual string FormatReservationProductPeriod(Product product, string price)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (product.ProductTypeId != ProductType.Reservation)
                return price;

            if (String.IsNullOrWhiteSpace(price))
                return price;

            string result;
            switch (product.IntervalUnitId)
            {
                case IntervalUnit.Day:
                    result = string.Format(_translationService.GetResource("Products.Price.Reservation.Days"), price, product.Interval);
                    break;
                case IntervalUnit.Hour:
                    result = string.Format(_translationService.GetResource("Products.Price.Reservation.Hour"), price, product.Interval);
                    break;
                case IntervalUnit.Minute:
                    result = string.Format(_translationService.GetResource("Products.Price.Reservation.Minute"), price, product.Interval);
                    break;
                default:
                    throw new GrandException("Not supported reservation period");
            }

            return result;
        }


        /// <summary>
        /// Payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        public virtual string FormatPaymentMethodAdditionalFee(double price)
        {
            bool priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
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
            bool showTax = _taxSettings.PaymentMethodAdditionalFeeIsTaxable && _taxSettings.DisplayTaxSuffix;
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
