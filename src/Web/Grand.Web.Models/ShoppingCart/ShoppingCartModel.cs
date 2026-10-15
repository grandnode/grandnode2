using Grand.Domain.Catalog;
using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Models.ShoppingCart;

public class ShoppingCartModel : BaseModel
{
    public bool ShowSku { get; set; }
    public bool ShowProductImages { get; set; }
    public bool IsEditable { get; set; }
    public bool IsAllowOnHold { get; set; }
    public bool TermsOfServicePopup { get; set; }
    public IList<ShoppingCartItemModel> Items { get; set; } = new List<ShoppingCartItemModel>();

    public string CheckoutAttributeInfo { get; set; }
    public IList<CheckoutAttributeModel> CheckoutAttributes { get; set; } = new List<CheckoutAttributeModel>();

    public IList<string> Warnings { get; set; } = new List<string>();
    public string MinOrderSubtotalWarning { get; set; }
    public bool ShowCheckoutAsGuestButton { get; set; }
    public bool IsGuest { get; set; }
    public bool TermsOfServiceOnShoppingCartPage { get; set; }
    public bool TermsOfServiceOnOrderConfirmPage { get; set; }
    public DiscountBoxModel DiscountBox { get; set; } = new();
    public GiftVoucherBoxModel GiftVoucherBox { get; set; } = new();

    #region Nested Classes

    public class ShoppingCartItemModel : BaseEntityModel
    {
        public string Sku { get; set; }
        public bool IsCart { get; set; }
        public PictureModel Picture { get; set; } = new();
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSeName { get; set; }
        public string ProductUrl { get; set; }
        public string WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string WarehouseCode { get; set; }
        public string VendorId { get; set; }
        public string VendorName { get; set; }
        public string VendorSeName { get; set; }
        public string UnitPriceWithoutDiscount { get; set; }
        public double UnitPriceWithoutDiscountValue { get; set; }
        public string UnitPrice { get; set; }
        public double UnitPriceValue { get; set; }
        public string SubTotal { get; set; }
        public double SubTotalValue { get; set; }
        public string Discount { get; set; }
        public int DiscountedQty { get; set; }
        public HashSet<string> Discounts { get; set; } = new();
        public int Quantity { get; set; }
        public List<SelectListItem> AllowedQuantities { get; set; } = new();
        public string AttributeInfo { get; set; }
        public string RecurringInfo { get; set; }
        public bool AllowItemEditing { get; set; }
        public bool DisableRemoval { get; set; }
        public string ReservationInfo { get; set; }
        public string AuctionInfo { get; set; }
        public string Parameter { get; set; }
        public IList<string> Warnings { get; set; } = new List<string>();
    }

    public class CheckoutAttributeModel : BaseEntityModel
    {
        public string Name { get; set; }

        public string DefaultValue { get; set; }

        public string TextPrompt { get; set; }

        public bool IsRequired { get; set; }

        /// <summary>
        ///     Allowed file extensions for customer uploaded files
        /// </summary>
        public IList<string> AllowedFileExtensions { get; set; } = new List<string>();

        public AttributeControlType AttributeControlType { get; set; }

        public IList<CheckoutAttributeValueModel> Values { get; set; } = new List<CheckoutAttributeValueModel>();
    }

    public class CheckoutAttributeValueModel : BaseEntityModel
    {
        public string Name { get; set; }

        public string ColorSquaresRgb { get; set; }

        public string PriceAdjustment { get; set; }

        public bool IsPreSelected { get; set; }
    }

    public class DiscountBoxModel : BaseModel
    {
        public List<DiscountInfoModel> AppliedDiscountsWithCodes { get; set; } = new();
        public bool Display { get; set; }
        public string Message { get; set; }
        public bool IsApplied { get; set; }

        public class DiscountInfoModel : BaseEntityModel
        {
            public string CouponCode { get; set; }
        }
    }

    public class GiftVoucherBoxModel : BaseModel
    {
        public bool Display { get; set; }
        public string Message { get; set; }
        public bool IsApplied { get; set; }
    }

    #endregion
}