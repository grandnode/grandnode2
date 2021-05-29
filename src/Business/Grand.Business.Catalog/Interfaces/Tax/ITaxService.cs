using Grand.Business.Catalog.Services.Tax;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Tax
{
    /// <summary>
    /// Tax service
    /// </summary>
    public partial interface ITaxService
    {
        /// <summary>
        /// Load active tax provider
        /// </summary>
        /// <returns>Active tax provider</returns>
        ITaxProvider LoadActiveTaxProvider();

        /// <summary>
        /// Load tax provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found tax provider</returns>
        ITaxProvider LoadTaxProviderBySystemName(string systemName);

        /// <summary>
        /// Load all tax providers
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store ident</param>
        /// <returns>Tax providers</returns>
        IList<ITaxProvider> LoadAllTaxProviders(Customer customer = null, string storeId = "");


        /// <summary>
        /// Gets price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="price">Price</param>
        /// <param name="taxRate">Tax rate</param>
        /// <returns>Price</returns>
        Task<(double productprice, double taxRate)> GetProductPrice(Product product, double price);

        /// <summary>
        /// Gets price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="price">Price</param>
        /// <param name="customer">Customer</param>
        /// <returns>Price</returns>
        Task<(double productprice, double taxRate)> GetProductPrice(Product product, double price, Customer customer);

        /// <summary>
        /// Gets price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="price">Price</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <param name="customer">Customer</param>
        /// <returns>Price</returns>
        Task<(double productprice, double taxRate)> GetProductPrice(Product product, double price, bool includingTax, Customer customer);

        /// <summary>
        /// Gets price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="taxCategoryId">Tax category identifier</param>
        /// <param name="price">Price</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <param name="customer">Customer</param>
        /// <param name="priceIncludesTax">A value indicating whether price already includes tax</param>
        /// <returns>Price</returns>
        Task<(double productprice, double taxRate)> GetProductPrice(Product product, string taxCategoryId, double price, bool includingTax, Customer customer, bool priceIncludesTax);


        /// <summary>
        /// Gets price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="unitPrice">Unit Price</param>
        /// <param name="unitPricewithoutDisc">Unit price without discount</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="subTotal">Sub-Total</param>
        /// <param name="discountAmount">Discount amount</param>
        /// <param name="priceIncludesTax">A value indicating whether price already includes tax</param>
        /// <returns>TaxProductPrice</returns>
        Task<TaxProductPrice> GetTaxProductPrice(
            Product product,
            Customer customer,
            double unitPrice,
            double unitPricewithoutDisc,
            int quantity,
            double subTotal,
            double discountAmount,
            bool priceIncludesTax
            );

        /// <summary>
        /// Gets shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="customer">Customer</param>
        /// <returns>Price</returns>
        Task<(double shippingPrice, double taxRate)> GetShippingPrice(double price, Customer customer);

        /// <summary>
        /// Gets shipping price
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <param name="customer">Customer</param>
        /// <returns>Price</returns>
        Task<(double shippingPrice, double taxRate)> GetShippingPrice(double price, bool includingTax, Customer customer);

        /// <summary>
        /// Gets payment method additional handling fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="customer">Customer</param>
        /// <returns>Price</returns>
        Task<(double paymentPrice, double taxRate)> GetPaymentMethodAdditionalFee(double price, Customer customer);

        /// <summary>
        /// Gets payment method additional handling fee
        /// </summary>
        /// <param name="price">Price</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <param name="customer">Customer</param>
        /// <returns>Price</returns>
        Task<(double paymentPrice, double taxRate)> GetPaymentMethodAdditionalFee(double price, bool includingTax, Customer customer);

        /// <summary>
        /// Gets checkout attribute value price
        /// </summary>
        /// <param name="ca">Checkout attribute</param>
        /// <param name="cav">Checkout attribute value</param>
        /// <returns>Price</returns>
        Task<(double checkoutPrice, double taxRate)> GetCheckoutAttributePrice(CheckoutAttribute ca, CheckoutAttributeValue cav);

        /// <summary>
        /// Gets checkout attribute value price
        /// </summary>
        /// <param name="ca">Checkout attribute</param>
        /// <param name="cav">Checkout attribute value</param>
        /// <param name="customer">Customer</param>
        /// <returns>Price</returns>
        Task<(double checkoutPrice, double taxRate)> GetCheckoutAttributePrice(CheckoutAttribute ca, CheckoutAttributeValue cav, Customer customer);

        /// <summary>
        /// Gets checkout attribute value price
        /// </summary>
        /// <param name="ca">Checkout attribute</param>
        /// <param name="cav">Checkout attribute value</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <param name="customer">Customer</param>
        /// <returns>Price</returns>
        Task<(double checkoutPrice, double taxRate)> GetCheckoutAttributePrice(CheckoutAttribute ca, CheckoutAttributeValue cav, bool includingTax, Customer customer);

        /// <summary>
        /// Gets a value indicating whether a product is tax exempt
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <returns>A value indicating whether a product is tax exempt</returns>
        Task<bool> IsTaxExempt(Product product, Customer customer);

        /// <summary>
        /// Gets a value indicating whether EU VAT exempt (the European Union Value Added Tax)
        /// </summary>
        /// <param name="address">Address</param>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        Task<bool> IsVatExempt(Address address, Customer customer);
    }
}
