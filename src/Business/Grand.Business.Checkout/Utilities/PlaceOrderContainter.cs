using Grand.Business.Catalog.Utilities;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using System.Collections.Generic;

namespace Grand.Business.Checkout.Utilities
{
    public class PlaceOrderContainter
    {
        public PlaceOrderContainter()
        {
            Cart = new List<ShoppingCartItem>();
            Taxes = new List<OrderTax>();
            AppliedDiscounts = new List<ApplyDiscount>();
            AppliedGiftVouchers = new List<AppliedGiftVoucher>();
            CheckoutAttributes = new List<CustomAttribute>();
        }

        public Customer Customer { get; set; }
        public Language Language { get; set; }
        public Currency Currency { get; set; }
        public string AffiliateId { get; set; }
        public TaxDisplayType TaxDisplayType { get; set; }
        public double CurrencyRate { get; set; }
        public string PrimaryCurrencyCode { get; set; }
        public string PaymentMethodSystemName { get; set; }
        public Address BillingAddress { get; set; }
        public Address ShippingAddress { get; set; }
        public ShippingStatus ShippingStatus { get; set; }
        public string ShippingMethodName { get; set; }
        public string ShippingRateProviderSystemName { get; set; }
        public bool PickUpInStore { get; set; }
        public PickupPoint PickupPoint { get; set; }
        
        public string CheckoutAttributeDescription { get; set; }

        public IList<CustomAttribute> CheckoutAttributes { get; set; }

        public IList<ShoppingCartItem> Cart { get; set; }
        public IList<OrderTax> Taxes { get; set; }

        public List<ApplyDiscount> AppliedDiscounts { get; set; }
        public List<AppliedGiftVoucher> AppliedGiftVouchers { get; set; }

        public double OrderSubTotalInclTax { get; set; }
        public double OrderSubTotalExclTax { get; set; }
        public double OrderSubTotalDiscountInclTax { get; set; }
        public double OrderSubTotalDiscountExclTax { get; set; }
        public double OrderShippingTotalInclTax { get; set; }
        public double OrderShippingTotalExclTax { get; set; }
        public double PaymentAdditionalFeeInclTax { get; set; }
        public double PaymentAdditionalFeeExclTax { get; set; }
        public double OrderTaxTotal { get; set; }
        public double OrderDiscountAmount { get; set; }
        public int RedeemedLoyaltyPoints { get; set; }
        public double RedeemedLoyaltyPointsAmount { get; set; }
        public double OrderTotal { get; set; }
    }

}
