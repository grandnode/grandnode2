using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Settings
{
    public class SalesSettingsModel : BaseModel
    {
        public SalesSettingsModel()
        {
            OrderSettings = new OrderSettingsModel();
            ShoppingCartSettings = new ShoppingCartSettingsModel();
            LoyaltyPointsSettings = new LoyaltyPointsSettingsModel();
        }

        public string ActiveStore { get; set; }
        public OrderSettingsModel OrderSettings { get; set; }
        public ShoppingCartSettingsModel ShoppingCartSettings { get; set; }
        public LoyaltyPointsSettingsModel LoyaltyPointsSettings { get; set; }

        public partial class OrderSettingsModel : BaseModel
        {
            public OrderSettingsModel()
            {
                GiftVouchers_Activated_OrderStatuses = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Settings.Order.IsReOrderAllowed")]
            public bool IsReOrderAllowed { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.MinOrderSubtotalAmount")]
            public double MinOrderSubtotalAmount { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.MinOrderSubtotalAmountIncludingTax")]
            public bool MinOrderSubtotalAmountIncludingTax { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.AnonymousCheckoutAllowed")]
            public bool AnonymousCheckoutAllowed { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.TermsOfServiceOnShoppingCartPage")]
            public bool TermsOfServiceOnShoppingCartPage { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.TermsOfServiceOnOrderConfirmPage")]
            public bool TermsOfServiceOnOrderConfirmPage { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.DisableOrderCompletedPage")]
            public bool DisableOrderCompletedPage { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.AttachPdfInvoiceToOrderPlacedEmail")]
            public bool AttachPdfInvoiceToOrderPlacedEmail { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.AttachPdfInvoiceToOrderPaidEmail")]
            public bool AttachPdfInvoiceToOrderPaidEmail { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.AttachPdfInvoiceToOrderCompletedEmail")]
            public bool AttachPdfInvoiceToOrderCompletedEmail { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnsEnabled")]
            public bool MerchandiseReturnsEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturns_AllowToSpecifyPickupAddress")]
            public bool MerchandiseReturns_AllowToSpecifyPickupAddress { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturns_AllowToSpecifyPickupDate")]
            public bool MerchandiseReturns_AllowToSpecifyPickupDate { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.NumberOfDaysMerchandiseReturnAvailable")]
            public int NumberOfDaysMerchandiseReturnAvailable { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.GiftVouchers_Activated")]
            public int GiftVouchers_Activated_OrderStatusId { get; set; }
            public IList<SelectListItem> GiftVouchers_Activated_OrderStatuses { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.DeactivateGiftVouchersAfterCancelOrder")]
            public bool DeactivateGiftVouchersAfterCancelOrder { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.DeactivateGiftVouchersAfterDeletingOrder")]
            public bool DeactivateGiftVouchersAfterDeletingOrder { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.CompleteOrderWhenDelivered")]
            public bool CompleteOrderWhenDelivered { get; set; }

            public string PrimaryStoreCurrencyCode { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.UserCanCancelUnpaidOrder")]
            public bool UserCanCancelUnpaidOrder { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.AllowCustomerToAddOrderNote")]
            public bool AllowCustomerToAddOrderNote { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.AttachPdfInvoiceToBinary")]
            public bool AttachPdfInvoiceToBinary { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturns_PickupDateRequired")]
            public bool MerchandiseReturns_PickupDateRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.MinOrderTotalAmount")]
            public double MinOrderTotalAmount { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.MinimumOrderPlacementInterval")]
            public int MinimumOrderPlacementInterval { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.LengthCode")]
            public int LengthCode { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.UnpublishAuctionProduct")]
            public bool UnpublishAuctionProduct { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Order.PageSize")]
            public int PageSize { get; set; }

        }

        public partial class ShoppingCartSettingsModel : BaseModel
        {
            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.DisplayCartAfterAddingProduct")]
            public bool DisplayCartAfterAddingProduct { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.DisplayWishlistAfterAddingProduct")]
            public bool DisplayWishlistAfterAddingProduct { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.MaximumShoppingCartItems")]
            public int MaximumShoppingCartItems { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.MaximumWishlistItems")]
            public int MaximumWishlistItems { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.AllowOutOfStockItemsToBeAddedToWishlist")]
            public bool AllowOutOfStockItemsToBeAddedToWishlist { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.MoveItemsFromWishlistToCart")]
            public bool MoveItemsFromWishlistToCart { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.ShowProductImagesOnShoppingCart")]
            public bool ShowProductImagesOnShoppingCart { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.ShowProductImagesOnWishList")]
            public bool ShowProductImagesOnWishList { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.ShowDiscountBox")]
            public bool ShowDiscountBox { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.ShowGiftVoucherBox")]
            public bool ShowGiftVoucherBox { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.CrossSellsNumber")]
            public int CrossSellsNumber { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.EmailWishlistEnabled")]
            public bool EmailWishlistEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.AllowAnonymousUsersToEmailWishlist")]
            public bool AllowAnonymousUsersToEmailWishlist { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.MiniShoppingCartEnabled")]
            public bool MiniShoppingCartEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.ShowProductImagesInMiniShoppingCart")]
            public bool ShowImagesInsidebarCart { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.MiniShoppingCartProductNumber")]
            public int MiniCartProductNumber { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.AllowCartItemEditing")]
            public bool AllowCartItemEditing { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.SharedCartBetweenStores")]
            public bool SharedCartBetweenStores { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.AllowOnHoldCart")]
            public bool AllowOnHoldCart { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.AllowToSelectWarehouse")]
            public bool AllowToSelectWarehouse { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.GroupTierPrices")]
            public bool GroupTierPrices { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.RoundPrices")]
            public bool RoundPrices { get; set; }

            [GrandResourceDisplayName("Admin.Settings.ShoppingCart.ReservationDateFormat")]
            public string ReservationDateFormat { get; set; }
        }

        public partial class LoyaltyPointsSettingsModel : BaseModel
        {
            public LoyaltyPointsSettingsModel()
            {
                PointsForPurchases_Awarded_OrderStatuses = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.Enabled")]
            public bool Enabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.ExchangeRate")]
            public double ExchangeRate { get; set; }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.MinimumLoyaltyPointsToUse")]
            public int MinimumLoyaltyPointsToUse { get; set; }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.PointsForRegistration")]
            public int PointsForRegistration { get; set; }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.PointsForPurchases_Amount")]
            public double PointsForPurchases_Amount { get; set; }
            public int PointsForPurchases_Points { get; set; }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.PointsForPurchases_Awarded")]
            public int PointsForPurchases_Awarded { get; set; }
            public IList<SelectListItem> PointsForPurchases_Awarded_OrderStatuses { get; set; }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.ReduceLoyaltyPointsAfterCancelOrder")]
            public bool ReduceLoyaltyPointsAfterCancelOrder { get; set; }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.DisplayHowMuchWillBeEarned")]
            public bool DisplayHowMuchWillBeEarned { get; set; }

            public string PrimaryStoreCurrencyCode { get; set; }

            [GrandResourceDisplayName("Admin.Settings.LoyaltyPoints.PointsAccumulatedForAllStores")]
            public bool PointsAccumulatedForAllStores { get; set; }

        }
    }
}
