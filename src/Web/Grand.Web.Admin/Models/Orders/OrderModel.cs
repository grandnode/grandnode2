using Grand.Domain.Catalog;
using Grand.Domain.Payments;
using Grand.Domain.Tax;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class OrderModel : BaseEntityModel
    {
        public OrderModel()
        {
            CustomValues = new Dictionary<string, object>();
            TaxRates = new List<TaxRate>();
            GiftVouchers = new List<GiftVoucher>();
            Items = new List<OrderItemModel>();
            UsedDiscounts = new List<UsedDiscountModel>();
            OrderStatuses = new List<SelectListItem>();
        }

        public bool IsLoggedInAsVendor { get; set; }

        //identifiers
        [GrandResourceDisplayName("Admin.Orders.Fields.ID")]
        public override string Id { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Fields.ID")]
        public int OrderNumber { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Code")]
        public string Code { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Fields.OrderGuid")]
        public Guid OrderGuid { get; set; }

        //store
        [GrandResourceDisplayName("Admin.Orders.Fields.Store")]
        public string StoreName { get; set; }

        //customer info
        [GrandResourceDisplayName("Admin.Orders.Fields.Customer")]
        public string CustomerId { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Customer")]
        public string CustomerInfo { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.CustomerEmail")]
        public string CustomerEmail { get; set; }
        public string CustomerFullName { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.CustomerIP")]
        public string CustomerIp { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.UrlReferrer")]
        public string UrlReferrer { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Fields.CustomValues")]
        public Dictionary<string, object> CustomValues { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Fields.Affiliate")]
        public string AffiliateId { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Affiliate")]
        public string AffiliateName { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Fields.SalesEmployee")]
        public string SalesEmployeeId { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.SalesEmployee")]
        public string SalesEmployeeName { get; set; }

        //Used discounts
        [GrandResourceDisplayName("Admin.Orders.Fields.UsedDiscounts")]
        public IList<UsedDiscountModel> UsedDiscounts { get; set; }

        //totals
        public TaxDisplayType TaxDisplayType { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderSubtotalInclTax")]
        public string OrderSubtotalInclTax { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderSubtotalExclTax")]
        public string OrderSubtotalExclTax { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderSubTotalDiscountInclTax")]
        public string OrderSubTotalDiscountInclTax { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderSubTotalDiscountExclTax")]
        public string OrderSubTotalDiscountExclTax { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderShippingInclTax")]
        public string OrderShippingInclTax { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderShippingExclTax")]
        public string OrderShippingExclTax { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.PaymentMethodAdditionalFeeInclTax")]
        public string PaymentMethodAdditionalFeeInclTax { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.PaymentMethodAdditionalFeeExclTax")]
        public string PaymentMethodAdditionalFeeExclTax { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Tax")]
        public string Tax { get; set; }
        public IList<TaxRate> TaxRates { get; set; }
        public bool DisplayTax { get; set; }
        public bool DisplayTaxRates { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderTotalDiscount")]
        public string OrderTotalDiscount { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.RedeemedLoyaltyPoints")]
        public int RedeemedLoyaltyPoints { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.RedeemedLoyaltyPoints")]
        public string RedeemedLoyaltyPointsAmount { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderTotal")]
        public string OrderTotal { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.RefundedAmount")]
        public string RefundedAmount { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.SuggestedRefundedAmount")]
        public string SuggestedRefundedAmount { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Profit")]
        public string Profit { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Currency")]
        public string CurrencyCode { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.CurrencyRate")]

        [UIHint("DoubleN4")]
        public double CurrencyRate { get; set; }
        //edit totals
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.OrderSubtotal")]
        public double OrderSubtotalInclTaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.OrderSubtotal")]
        public double OrderSubtotalExclTaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.OrderSubTotalDiscount")]
        public double OrderSubTotalDiscountInclTaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.OrderSubTotalDiscount")]
        public double OrderSubTotalDiscountExclTaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.OrderShipping")]
        public double OrderShippingInclTaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.OrderShipping")]
        public double OrderShippingExclTaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.PaymentMethodAdditionalFee")]
        public double PaymentMethodAdditionalFeeInclTaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.PaymentMethodAdditionalFee")]
        public double PaymentMethodAdditionalFeeExclTaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.Tax")]
        public double TaxValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.TaxRates")]
        public string TaxRatesValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.OrderTotalDiscount")]
        public double OrderTotalDiscountValue { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.Edit.OrderTotal")]
        public double OrderTotalValue { get; set; }

        //order status
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderStatus")]
        public string OrderStatus { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderStatus")]
        public int OrderStatusId { get; set; }
        public IList<SelectListItem> OrderStatuses { get; set; }
        //payment info
        [GrandResourceDisplayName("Admin.Orders.Fields.PaymentStatus")]
        public string PaymentStatus { get; set; }
        public PaymentStatus PaymentStatusEnum { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.PaymentMethod")]
        public string PaymentMethod { get; set; }
        public string PaymentTransactionId { get; set; }

        //shipping info
        public bool IsShippable { get; set; }
        public bool PickUpInStore { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Fields.PickupAddress")]
        public AddressModel PickupAddress { get; set; }

        [GrandResourceDisplayName("Admin.Orders.Fields.ShippingStatus")]
        public string ShippingStatus { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.ShippingAddress")]
        public AddressModel ShippingAddress { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.ShippingMethod")]
        public string ShippingMethod { get; set; }
        public string ShippingAdditionDescription { get; set; }
        public string ShippingAddressGoogleMapsUrl { get; set; }
        public bool CanAddNewShipments { get; set; }

        //billing info
        [GrandResourceDisplayName("Admin.Orders.Fields.BillingAddress")]
        public AddressModel BillingAddress { get; set; }
        [GrandResourceDisplayName("Admin.Orders.Fields.VatNumber")]
        public string VatNumber { get; set; }

        //gift vouchers
        public IList<GiftVoucher> GiftVouchers { get; set; }

        //items
        public bool HasDownloadableProducts { get; set; }
        public IList<OrderItemModel> Items { get; set; }

        //creation date
        [GrandResourceDisplayName("Admin.Orders.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        //checkout attributes
        public string CheckoutAttributeInfo { get; set; }


        //order notes
        [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.DisplayToCustomer")]
        public bool AddOrderNoteDisplayToCustomer { get; set; }
        [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.Note")]

        public string AddOrderNoteMessage { get; set; }
        public bool AddOrderNoteHasDownload { get; set; }
        [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.Download")]
        [UIHint("Download")]
        public string AddOrderNoteDownloadId { get; set; }

        //refund info
        [GrandResourceDisplayName("Admin.Orders.Fields.PartialRefund.AmountToRefund")]
        public double AmountToRefund { get; set; }
        public double MaxAmountToRefund { get; set; }
        public string PrimaryStoreCurrencyCode { get; set; }

        //workflow info
        public bool CanCancelOrder { get; set; }
       

        //order's tags
        [GrandResourceDisplayName("Admin.Orders.Fields.OrderTags")]
        public string OrderTags { get; set; }

        #region Nested Classes

        public partial class OrderItemModel : BaseEntityModel
        {
            public OrderItemModel()
            {
                MerchandiseReturnIds = new List<string>();
                PurchasedGiftVoucherIds = new List<string>();
            }
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string VendorName { get; set; }
            public string Sku { get; set; }

            public string PictureThumbnailUrl { get; set; }

            public string UnitPriceInclTax { get; set; }
            public string UnitPriceExclTax { get; set; }
            public double UnitPriceInclTaxValue { get; set; }
            public double UnitPriceExclTaxValue { get; set; }

            public int Quantity { get; set; }
            public int OpenQty { get; set; }
            public int CancelQty { get; set; }
            public int ShipQty { get; set; }
            public int ReturnQty { get; set; }

            public string DiscountInclTax { get; set; }
            public string DiscountExclTax { get; set; }
            public double DiscountInclTaxValue { get; set; }
            public double DiscountExclTaxValue { get; set; }

            public string SubTotalInclTax { get; set; }
            public string SubTotalExclTax { get; set; }
            public double SubTotalInclTaxValue { get; set; }
            public double SubTotalExclTaxValue { get; set; }

            public string AttributeInfo { get; set; }
            public string RecurringInfo { get; set; }
            public string RentalInfo { get; set; }
            public IList<string> MerchandiseReturnIds { get; set; }
            public IList<string> PurchasedGiftVoucherIds { get; set; }

            public bool IsDownload { get; set; }
            public int DownloadCount { get; set; }
            public DownloadActivationType DownloadActivationType { get; set; }
            public bool IsDownloadActivated { get; set; }
            public Guid LicenseDownloadGuid { get; set; }

            public string Commission { get; set; }
            public double CommissionValue { get; set; }
        }

        public partial class TaxRate : BaseModel
        {
            public string Rate { get; set; }
            public string Value { get; set; }
        }

        public partial class GiftVoucher : BaseModel
        {
            [GrandResourceDisplayName("Admin.Orders.Fields.GiftVoucherInfo")]
            public string CouponCode { get; set; }
            public string Amount { get; set; }
        }

        public partial class OrderNote : BaseEntityModel
        {
            public string OrderId { get; set; }
            [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.DisplayToCustomer")]
            public bool DisplayToCustomer { get; set; }
            [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.Note")]
            public string Note { get; set; }
            [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.Download")]
            public string DownloadId { get; set; }
            [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.Download")]
            public Guid DownloadGuid { get; set; }
            [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.CreatedOn")]
            public DateTime CreatedOn { get; set; }
            [GrandResourceDisplayName("Admin.Orders.OrderNotes.Fields.CreatedByCustomer")]
            public bool CreatedByCustomer { get; set; }
        }

        public partial class UploadLicenseModel : BaseModel
        {
            public string OrderId { get; set; }

            public string OrderItemId { get; set; }

            [UIHint("Download")]
            public string LicenseDownloadId { get; set; }

        }

        public partial class AddOrderProductModel : BaseModel
        {
            public AddOrderProductModel()
            {
                AvailableProductTypes = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]
            public string SearchProductName { get; set; }

            [UIHint("Category")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            [UIHint("Collection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }

            public IList<SelectListItem> AvailableCollections { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string OrderId { get; set; }
            public int OrderNumber { get; set; }
            #region Nested classes

            public partial class ProductModel : BaseEntityModel
            {
                [GrandResourceDisplayName("Admin.Orders.Products.AddNew.Name")]

                public string Name { get; set; }

                [GrandResourceDisplayName("Admin.Orders.Products.AddNew.SKU")]

                public string Sku { get; set; }
            }

            public partial class ProductDetailsModel : BaseModel
            {
                public ProductDetailsModel()
                {
                    ProductAttributes = new List<ProductAttributeModel>();
                    GiftVoucher = new GiftVoucherModel();
                    Warnings = new List<string>();
                }

                public string ProductId { get; set; }

                public string OrderId { get; set; }
                public int OrderNumber { get; set; }

                public ProductType ProductType { get; set; }

                public string Name { get; set; }

                [GrandResourceDisplayName("Admin.Orders.Products.AddNew.UnitPriceInclTax")]
                public double UnitPriceInclTax { get; set; }
                [GrandResourceDisplayName("Admin.Orders.Products.AddNew.UnitPriceExclTax")]
                public double UnitPriceExclTax { get; set; }

                [GrandResourceDisplayName("Admin.Orders.Products.AddNew.Quantity")]
                public int Quantity { get; set; }

                [GrandResourceDisplayName("Admin.Orders.Products.AddNew.TaxRate")]
                public double TaxRate { get; set; }

                //product attributes
                public IList<ProductAttributeModel> ProductAttributes { get; set; }
                //gift voucher info
                public GiftVoucherModel GiftVoucher { get; set; }

                public List<string> Warnings { get; set; }

            }

            public partial class ProductAttributeModel : BaseEntityModel
            {
                public ProductAttributeModel()
                {
                    Values = new List<ProductAttributeValueModel>();
                }

                public string ProductAttributeId { get; set; }

                public string Name { get; set; }

                public string TextPrompt { get; set; }

                public bool IsRequired { get; set; }

                public AttributeControlType AttributeControlType { get; set; }

                public IList<ProductAttributeValueModel> Values { get; set; }
            }

            public partial class ProductAttributeValueModel : BaseEntityModel
            {
                public string Name { get; set; }

                public bool IsPreSelected { get; set; }
            }


            public partial class GiftVoucherModel : BaseModel
            {
                public bool IsGiftVoucher { get; set; }

                [GrandResourceDisplayName("Admin.Orders.Products.GiftVoucher.RecipientName")]

                public string RecipientName { get; set; }
                [GrandResourceDisplayName("Admin.Orders.Products.GiftVoucher.RecipientEmail")]

                public string RecipientEmail { get; set; }
                [GrandResourceDisplayName("Admin.Orders.Products.GiftVoucher.SenderName")]

                public string SenderName { get; set; }
                [GrandResourceDisplayName("Admin.Orders.Products.GiftVoucher.SenderEmail")]

                public string SenderEmail { get; set; }
                [GrandResourceDisplayName("Admin.Orders.Products.GiftVoucher.Message")]

                public string Message { get; set; }

                public GiftVoucherType GiftVoucherType { get; set; }
            }
            #endregion
        }

        public partial class UsedDiscountModel : BaseModel
        {
            public string DiscountId { get; set; }
            public string DiscountName { get; set; }
        }

        #endregion
    }
}
