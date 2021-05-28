using Grand.Business.Checkout.Services.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Orders
{
    public interface IShoppingCartValidator
    {
        /// <summary>
        /// Validates a product for standard properties
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="product">Product</param>
        /// <param name="shoppingCartItem">Shopping cart item</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetStandardWarnings(Customer customer, Product product, ShoppingCartItem shoppingCartItem);

        /// <summary>
        /// Validates shopping cart item attributes
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="product">Product</param>
        /// <param name="shoppingCartItem">Shopping cart item</param>
        /// <param name="ignoreNonCombinableAttributes">A value indicating whether we should ignore non-combinable attributes</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetShoppingCartItemAttributeWarnings(Customer customer,
            Product product, ShoppingCartItem shoppingCartItem,
            bool ignoreNonCombinableAttributes = false);

        /// <summary>
        /// Validates shopping cart item (gift voucher)
        /// </summary>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="product">Product</param>
        /// <param name="Attributes">Attributes</param>
        /// <returns>Warnings</returns>
        IList<string> GetShoppingCartItemGiftVoucherWarnings(ShoppingCartType shoppingCartType,
            Product product, IList<CustomAttribute> attributes);

        /// <summary>
        /// Validate bid
        /// </summary>
        /// <param name="bid"></param>
        /// <param name="product"></param>
        /// <param name="customer"></param>
        /// <returns></returns>
        IList<string> GetAuctionProductWarning(double bid, Product product, Customer customer);

        /// <summary>
        /// Validates shopping cart item
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartItem">Shopping cart type</param>
        /// <param name="product">Product</param>
        /// <param name="options">Options</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetShoppingCartItemWarnings(Customer customer, ShoppingCartItem shoppingCartItem,
            Product product, ShoppingCartValidatorOptions options);

        /// <summary>
        /// Validates whether this shopping cart is valid
        /// </summary>
        /// <param name="shoppingCart">Shopping cart</param>
        /// <param name="checkoutAttributes">Checkout attributes</param>
        /// <param name="validateCheckoutAttributes">A value indicating whether to validate checkout attributes</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetShoppingCartWarnings(IList<ShoppingCartItem> shoppingCart,
            IList<CustomAttribute> checkoutAttributes, bool validateCheckoutAttributes);

        /// <summary>
        /// Validates shopping cart item for reservation products
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="Product">product</param>
        /// <param name="shoppingCartItem">ShoppingCartItem</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetReservationProductWarnings(Customer customer, Product product, ShoppingCartItem shoppingCartItem);

        /// <summary>
        /// Validates common problems
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="currentCart">Current cart</param>
        /// <param name="product">product</param>
        /// <param name="warnings">warnings</param>
        /// <param name="shoppingCartType">cart type</param>
        /// <param name="rentalStartDate">rental start date</param>
        /// <param name="rentalEndDate">rental end date</param>
        /// <param name="quantity">quantity</param>
        /// <param name="reservationId">reservation Id</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> CheckCommonWarnings(Customer customer, IList<ShoppingCartItem> currentCart, Product product,
          ShoppingCartType shoppingCartType, DateTime? rentalStartDate, DateTime? rentalEndDate, int quantity, string reservationId);

        /// <summary>
        /// Validates required products (products which require some other products to be added to the cart)
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="product">Product</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetRequiredProductWarnings(Customer customer,
            ShoppingCartType shoppingCartType, Product product,
            string storeId);
    }
}
