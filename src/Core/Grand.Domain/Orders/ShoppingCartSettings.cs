using Grand.Domain.Configuration;

namespace Grand.Domain.Orders
{
    public class ShoppingCartSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether a custoemr should be redirected to the shopping cart page after adding a product to the cart/wishlist
        /// </summary>
        public bool DisplayCartAfterAddingProduct { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a custoemr should be redirected to the shopping cart page after adding a product to the cart/wishlist
        /// </summary>
        public bool DisplayWishlistAfterAddingProduct { get; set; }

        /// <summary>
        /// Gets or sets a value indicating maximum number of items in the shopping cart
        /// </summary>
        public int MaximumShoppingCartItems { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating maximum number of items in the wishlist
        /// </summary>
        public int MaximumWishlistItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show product images in the mini-shopping cart block
        /// </summary>
        public bool AllowOutOfStockItemsToBeAddedToWishlist { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to move items from wishlist to cart when clicking "Add to cart" button. Otherwise, they are copied.
        /// </summary>
        public bool MoveItemsFromWishlistToCart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show product image on shopping cart page
        /// </summary>
        public bool ShowProductImagesOnShoppingCart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show product image on wishlist page
        /// </summary>
        public bool ShowProductImagesOnWishList { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show discount box on shopping cart page
        /// </summary>
        public bool ShowDiscountBox { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show gift voucher box on shopping cart page
        /// </summary>
        public bool ShowGiftVoucherBox { get; set; }

        /// <summary>
        /// Gets or sets a number of "Cross-sells" on shopping cart page
        /// </summary>
        public int CrossSellsNumber { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether "email a wishlist" feature is enabled
        /// </summary>
        public bool EmailWishlistEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enabled "email a wishlist" for guests.
        /// </summary>
        public bool AllowAnonymousUsersToEmailWishlist { get; set; }
        
        /// <summary>Gets or sets a value indicating whether mini-shopping cart is enabled
        /// </summary>
        public bool MiniShoppingCartEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show images in the sidebar cart block
        /// </summary>
        public bool ShowImagesInsidebarCart { get; set; }

        /// <summary>Gets or sets a maximum number of products which can be displayed in the mini-shopping cart block
        /// </summary>
        public int MiniCartProductNumber { get; set; }
        
        //Round is already an issue. 
        /// <summary>
        /// Gets or sets a value indicating whether to round calculated prices and total during calculation
        /// </summary>
        public bool RoundPrices { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should group cart items for the same products
        /// (a customer have two items in the cart for the same products with (different product attributes)
        /// </summary>
        public bool GroupTierPrices  { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a customer will beable to edit products in the cart
        /// </summary>
        public bool AllowCartItemEditing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a customer will be able to on hold product in the cart
        /// </summary>
        public bool AllowOnHoldCart { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether shopping carts (and wishlist) are shared between stores (in multi-store environment)
        /// </summary>
        public bool SharedCartBetweenStores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a customer will be able to select warehouse before add to cart
        /// </summary>
        public bool AllowToSelectWarehouse { get; set; }

        /// <summary>
        /// Gets or sets a value reservation format date 
        /// </summary>
        public string ReservationDateFormat { get; set; }
    }
}