using Grand.Domain.Catalog;
using Grand.Domain.Payments;
using Grand.Domain.Tax;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Vendor.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Orders;

public class OrderModel : BaseEntityModel
{
    //identifiers
    [GrandResourceDisplayName("Vendor.Orders.Fields.ID")]
    public override string Id { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.ID")]
    public int OrderNumber { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.Code")]
    public string Code { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.OrderGuid")]
    public Guid OrderGuid { get; set; }

    //store
    [GrandResourceDisplayName("Vendor.Orders.Fields.Store")]
    public string StoreName { get; set; }

    //customer info
    [GrandResourceDisplayName("Vendor.Orders.Fields.Customer")]
    public string CustomerInfo { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.CustomerEmail")]
    public string CustomerEmail { get; set; }

    public string CustomerFullName { get; set; }

    //order status
    [GrandResourceDisplayName("Admin.Orders.Fields.OrderStatus")]
    public string OrderStatus { get; set; }

    public int OrderStatusId { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.Currency")]
    public string CurrencyCode { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.CurrencyRate")]
    [UIHint("DoubleN4")]
    public double CurrencyRate { get; set; }

    public TaxDisplayType TaxDisplayType { get; set; }

    //payment info
    [GrandResourceDisplayName("Vendor.Orders.Fields.PaymentStatus")]
    public string PaymentStatus { get; set; }

    public PaymentStatus PaymentStatusEnum { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.PaymentMethod")]
    public string PaymentMethod { get; set; }

    //shipping info
    public bool IsShippable { get; set; }
    public bool PickUpInStore { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.PickupAddress")]
    public AddressModel PickupAddress { get; set; }


    [GrandResourceDisplayName("Vendor.Orders.Fields.ShippingAddress")]
    public AddressModel ShippingAddress { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.ShippingMethod")]
    public string ShippingMethod { get; set; }

    public string ShippingAdditionDescription { get; set; }
    public string ShippingAddressGoogleMapsUrl { get; set; }
    public bool CanAddNewShipments { get; set; }

    //billing info
    [GrandResourceDisplayName("Vendor.Orders.Fields.BillingAddress")]
    public AddressModel BillingAddress { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Fields.VatNumber")]
    public string VatNumber { get; set; }

    //items
    public bool HasDownloadableProducts { get; set; }
    public IList<OrderItemModel> Items { get; set; } = new List<OrderItemModel>();

    //creation date
    [GrandResourceDisplayName("Vendor.Orders.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    //checkout attributes
    public string CheckoutAttributeInfo { get; set; }


    #region Nested Classes

    public class OrderItemModel : BaseEntityModel
    {
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

        public IList<string> MerchandiseReturnIds { get; set; } = new List<string>();
        public IList<string> PurchasedGiftVoucherIds { get; set; } = new List<string>();

        public bool IsDownload { get; set; }
        public int DownloadCount { get; set; }
        public DownloadActivationType DownloadActivationType { get; set; }
        public bool IsDownloadActivated { get; set; }
        public Guid LicenseDownloadGuid { get; set; }

        public string Commission { get; set; }
        public double CommissionValue { get; set; }
    }

    #endregion
}