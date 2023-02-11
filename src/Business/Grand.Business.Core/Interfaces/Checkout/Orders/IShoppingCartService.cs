using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders
{
    /// <summary>
    /// Shopping cart service
    /// </summary>
    public interface IShoppingCartService
    {

        /// <summary>
        /// Gets shopping cart
        /// </summary>
        /// <param name="storeId">Store identifier; pass null to load all records</param>
        /// <param name="shoppingCartType">Shopping cart type; pass null to load all records</param>
        /// <returns>Shopping Cart</returns>
        Task<IList<ShoppingCartItem>> GetShoppingCart(string storeId = null, params ShoppingCartType[] shoppingCartType);

        /// <summary>
        /// Finds a shopping cart item in the cart
        /// </summary>
        /// <param name="shoppingCart">Shopping cart</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="productId">Product ident</param>
        /// <param name="warehouseId">Warehouse ident</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="customerEnteredPrice">Price entered by a customer</param>
        /// <param name="rentalStartDate">Rental start date</param>
        /// <param name="rentalEndDate">Rental end date</param>
        /// <returns>Found shopping cart item</returns>
        Task<ShoppingCartItem> FindShoppingCartItem(IList<ShoppingCartItem> shoppingCart,
            ShoppingCartType shoppingCartType,
            string productId,
            string warehouseId = null,
            IList<CustomAttribute> attributes = null,
            double? customerEnteredPrice = null,
            DateTime? rentalStartDate = null,
            DateTime? rentalEndDate = null);

        /// <summary>
        /// Add a product to shopping cart
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="productId">Product id</param>
        /// <param name="shoppingCartType">ShoppingCartType</param>
        /// <param name="storeId">Store id</param>
        /// <param name="warehouseId">Warehouse id</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="customerEnteredPrice">EnteredPrice</param>
        /// <param name="rentalStartDate">RentalStartDate</param>
        /// <param name="rentalEndDate">RentalEndDate</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="automaticallyAddRequiredProductsIfEnabled">AutomaticallyAddRequiredProductsIfEnabled</param>
        /// <param name="reservationId">ReservationId</param>
        /// <param name="parameter">Parameter for reservation</param>
        /// <param name="duration">Duration for reservation</param>
        /// <param name="validator">ShoppingCartValidatorOptions</param>
        /// <returns>(warnings, shoppingCartItem)</returns>
        Task<(IList<string> warnings, ShoppingCartItem shoppingCartItem)> AddToCart(Customer customer, string productId,
            ShoppingCartType shoppingCartType, string storeId, string warehouseId = null, IList<CustomAttribute> attributes = null,
            double? customerEnteredPrice = null,
            DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
            int quantity = 1,
            bool automaticallyAddRequiredProductsIfEnabled = true, string reservationId = "", string parameter = "", string duration = "",
            ShoppingCartValidatorOptions validator = null);

        /// <summary>
        /// Updates the shopping cart item
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartItemId">Shopping cart item identifier</param>
        /// <param name="warehouseId">Warehouse ident</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="customerEnteredPrice">New customer entered price</param>
        /// <param name="rentalStartDate">Rental start date</param>
        /// <param name="rentalEndDate">Rental end date</param>
        /// <param name="quantity">New shopping cart item quantity</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset checkout data</param>
        /// <param name="reservationId">Reservation ident</param>
        /// <param name="sciId"></param>
        /// <returns>Warnings</returns>
        Task<IList<string>> UpdateShoppingCartItem(Customer customer,
            string shoppingCartItemId, string warehouseId, IList<CustomAttribute> attributes,
            double? customerEnteredPrice = null,
            DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
            int quantity = 1, bool resetCheckoutData = true, string reservationId = "", string sciId = "");

        /// <summary>
        /// Delete shopping cart item
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartItem">Shopping cart item</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset checkout data</param>
        /// <param name="ensureOnlyActiveCheckoutAttributes">A value indicating whether to ensure that only active checkout attributes are attached to the current customer</param>
        Task DeleteShoppingCartItem(Customer customer, ShoppingCartItem shoppingCartItem, bool resetCheckoutData = true,
            bool ensureOnlyActiveCheckoutAttributes = false);

        /// <summary>
        /// Migrate shopping cart
        /// </summary>
        /// <param name="fromCustomer">From customer</param>
        /// <param name="toCustomer">To customer</param>
        /// <param name="includeCouponCodes">A value indicating whether to coupon codes (discount and gift voucher) should be also re-applied</param>
        Task MigrateShoppingCart(Customer fromCustomer, Customer toCustomer, bool includeCouponCodes);
    }
}
