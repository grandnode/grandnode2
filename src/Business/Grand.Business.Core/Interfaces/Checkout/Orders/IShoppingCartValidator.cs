using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

public interface IShoppingCartValidator
{
    /// <summary>
    ///     Validates a product for standard properties
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="product">Product</param>
    /// <param name="shoppingCartItem">Shopping cart item</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> GetStandardWarnings(Customer customer, Product product, ShoppingCartItem shoppingCartItem,
        Language language);

    /// <summary>
    ///     Validates shopping cart item attributes
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="product">Product</param>
    /// <param name="shoppingCartItem">Shopping cart item</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <param name="ignoreNonCombinableAttributes">A value indicating whether we should ignore non-combinable attributes</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> GetShoppingCartItemAttributeWarnings(Customer customer,
        Product product, ShoppingCartItem shoppingCartItem, Language language,
        bool ignoreNonCombinableAttributes = false);

    /// <summary>
    ///     Validates shopping cart item (gift voucher)
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="product">Product</param>
    /// <param name="shoppingCartItem">Shopping cart item</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> GetShoppingCartItemGiftVoucherWarnings(Customer customer,
        Product product, ShoppingCartItem shoppingCartItem, Language language);

    /// <summary>
    ///     Validate bid
    /// </summary>
    /// <param name="bid"></param>
    /// <param name="product"></param>
    /// <param name="customer"></param>
    /// <param name="language">Language the warnings are written in</param>
    /// <returns></returns>
    Task<IList<string>> GetAuctionProductWarning(double bid, Product product, Customer customer, Language language);

    /// <summary>
    ///     Validates shopping cart item
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="shoppingCartItem">Shopping cart type</param>
    /// <param name="product">Product</param>
    /// <param name="options">Options</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> GetShoppingCartItemWarnings(Customer customer, ShoppingCartItem shoppingCartItem,
        Product product, ShoppingCartValidatorOptions options, Language language);

    /// <summary>
    ///     Validates whether this shopping cart is valid
    /// </summary>
    /// <param name="shoppingCart">Shopping cart</param>
    /// <param name="checkoutAttributes">Checkout attributes</param>
    /// <param name="context">Customer, store, currency and tax display the cart is validated for</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <param name="validateCheckoutAttributes">A value indicating whether to validate checkout attributes</param>
    /// <param name="validateAmount">A value indicating whether to validate subtotal/total attributes</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> GetShoppingCartWarnings(IList<ShoppingCartItem> shoppingCart,
        IList<CustomAttribute> checkoutAttributes, CalculationContext context, Language language,
        bool validateCheckoutAttributes, bool validateAmount);

    /// <summary>
    ///     Validates shopping cart item for reservation products
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="product">product</param>
    /// <param name="shoppingCartItem">ShoppingCartItem</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> GetReservationProductWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem, Language language);

    /// <summary>
    ///     Validates shopping cart item for inventory
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="product">product</param>
    /// <param name="shoppingCartItem">ShoppingCartItem</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> GetInventoryProductWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem, Language language);

    /// <summary>
    ///     Validates common problems
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="currentCart">Current cart</param>
    /// <param name="product">product</param>
    /// <param name="shoppingCartType">cart type</param>
    /// <param name="rentalStartDate">rental start date</param>
    /// <param name="rentalEndDate">rental end date</param>
    /// <param name="quantity">quantity</param>
    /// <param name="reservationId">reservation Id</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> CheckCommonWarnings(Customer customer, IList<ShoppingCartItem> currentCart, Product product,
        ShoppingCartType shoppingCartType, DateTime? rentalStartDate, DateTime? rentalEndDate, int quantity,
        string reservationId, Language language);

    /// <summary>
    ///     Validates required products (products which require some other products to be added to the cart)
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="shoppingCartItem">Shopping cart type</param>
    /// <param name="product">Product</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="language">Language the warnings are written in</param>
    /// <returns>Warnings</returns>
    Task<IList<string>> GetRequiredProductWarnings(Customer customer,
        ShoppingCartItem shoppingCartItem, Product product,
        string storeId, Language language);
}