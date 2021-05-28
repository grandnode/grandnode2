using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Prices
{
    public partial interface IPriceFormatter
    {
        /// <summary>
        /// Formats price
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        string FormatPrice(double price);

        /// <summary>
        /// Formats price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <returns>Price</returns>
        string FormatPrice(double price, Currency targetCurrency);

        /// <summary>
        /// Formats price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="showTax">A value that indicates if it should shows tax suffix</param>
        /// <returns>Price</returns>
        string FormatPrice(double price, bool showTax);

        /// <summary>
        /// Formats price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="showTax">A value that indicates if it should shows a tax suffix</param>
        /// <param name="language">Language</param>
        /// <returns>Price</returns>
        Task<string> FormatPrice(double price, string currencyCode, bool showTax, Language language);

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <returns>Price</returns>
        Task<string> FormatPrice(double price, string currencyCode, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <returns>Price</returns>
        string FormatPrice(double price, Currency targetCurrency, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <param name="showTax">A value indicating whether to show tax suffix</param>
        /// <returns>Price</returns>
        string FormatPrice(double price, Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax);

        /// <summary>
        /// Formats the price of rental product (with rental period)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="price">Price</param>
        /// <returns>Rental product price with period</returns>
        string FormatReservationProductPeriod(Product product, string price);

        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        string FormatShippingPrice(double price);

        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <returns>Price</returns>
        string FormatShippingPrice(double price, Currency targetCurrency, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <param name="showTax">A value indicating whether to show tax suffix</param>
        /// <returns>Price</returns>
        string FormatShippingPrice(double price, Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax);

        /// <summary>
        /// Formats the shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <returns>Price</returns>
        Task<string> FormatShippingPrice(double price, string currencyCode, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        string FormatPaymentMethodAdditionalFee(double price);

        /// <summary>
        /// Formats the payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <returns>Price</returns>
        string FormatPaymentMethodAdditionalFee(double price, Currency targetCurrency, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats the payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="targetCurrency">Target currency</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <param name="showTax">A value indicating whether to show tax suffix</param>
        /// <returns>Price</returns>
        string FormatPaymentMethodAdditionalFee(double price, Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax);

        /// <summary>
        /// Formats the payment method additional fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="language">Language</param>
        /// <param name="priceIncludesTax">A value indicating whether price includes tax</param>
        /// <returns>Price</returns>
        Task<string> FormatPaymentMethodAdditionalFee(double price, string currencyCode, Language language, bool priceIncludesTax);

        /// <summary>
        /// Formats a tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        /// <returns>Formatted tax rate</returns>
        string FormatTaxRate(double taxRate);
    }
}
