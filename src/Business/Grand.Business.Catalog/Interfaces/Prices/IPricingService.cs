using Grand.Business.Catalog.Utilities;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Prices
{
    /// <summary>
    /// Price calculation service
    /// </summary>
    public partial interface IPricingService
    {
        /// <summary>
        /// Gets the final price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="currency">The currency</param>
        /// <param name="additionalCharge">Additional charge</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
        /// <param name="quantity">Shopping cart item quantity</param>
        /// <returns>Final price</returns>
        Task<(double finalPrice, double discountAmount, List<ApplyDiscount> appliedDiscounts, TierPrice preferredTierPrice)> GetFinalPrice(
            Product product,
            Customer customer,
            Currency currency,
            double additionalCharge = 0, 
            bool includeDiscounts = true, 
            int quantity = 1);

        /// <summary>
        /// Gets the final price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="currency">The currency</param>
        /// <param name="additionalCharge">Additional charge</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
        /// <param name="quantity">Shopping cart item quantity</param>
        /// <param name="rentalStartDate">Rental period start date (for rental products)</param>
        /// <param name="rentalEndDate">Rental period end date (for rental products)</param>
        /// <returns>Final price</returns>
        Task<(double finalPrice, double discountAmount, List<ApplyDiscount> appliedDiscounts, TierPrice preferredTierPrice)> GetFinalPrice(Product product,
            Customer customer,
            Currency currency,
            double additionalCharge,
            bool includeDiscounts,
            int quantity,
            DateTime? rentalStartDate,
            DateTime? rentalEndDate);



        /// <summary>
        /// Gets the shopping cart unit price (one item)
        /// </summary>
        /// <param name="shoppingCartItem">The shopping cart item</param>
        /// <param name="product">Product</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <returns>Shopping cart unit price (one item)</returns>
        Task<(double unitprice, double discountAmount, List<ApplyDiscount> appliedDiscounts)> GetUnitPrice(ShoppingCartItem shoppingCartItem,
            Product product, bool includeDiscounts = true);


        /// <summary>
        /// Gets the shopping cart unit price (one item)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">The currency</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="attributes">Product atrributes</param>
        /// <param name="customerEnteredPrice">Customer entered price (if specified)</param>
        /// <param name="rentalStartDate">Rental start date (null for not rental products)</param>
        /// <param name="rentalEndDate">Rental end date (null for not rental products)</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <returns>Shopping cart unit price (one item)</returns>
        Task<(double unitprice, double discountAmount, List<ApplyDiscount> appliedDiscounts)> GetUnitPrice(Product product,
            Customer customer,
            Currency currency,
            ShoppingCartType shoppingCartType,
            int quantity,
            IList<CustomAttribute> attributes,
            double? customerEnteredPrice,
            DateTime? rentalStartDate, DateTime? rentalEndDate,
            bool includeDiscounts);

        /// <summary>
        /// Gets the shopping cart item sub total
        /// </summary>
        /// <param name="shoppingCartItem">The shopping cart item</param>
        /// <param name="product">Product</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <returns>Shopping cart item sub total</returns>
        Task<(double subTotal, double discountAmount, List<ApplyDiscount> appliedDiscounts)> GetSubTotal(ShoppingCartItem shoppingCartItem, Product product,
            bool includeDiscounts = true);

        /// <summary>
        /// Gets the product cost (one item)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Shopping cart item attributes</param>
        /// <returns>Product cost (one item)</returns>
        Task<double> GetProductCost(Product product, IList<CustomAttribute> attributes);

        
        /// <summary>
        /// Get a price adjustment of a product attribute value
        /// </summary>
        /// <param name="value">Product attribute value</param>
        /// <returns>Price adjustment</returns>
        Task<double> GetProductAttributeValuePriceAdjustment(ProductAttributeValue value);
    }
}
