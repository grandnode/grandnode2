using Grand.Domain.Common;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents an order
    /// </summary>
    public partial class Order : BaseEntity
    {
        private ICollection<OrderItem> _orderItems;
        private ICollection<OrderTax> _orderTaxes;
        private ICollection<string> _orderTags;

        #region Properties

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public Guid OrderGuid { get; set; }

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public int OrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the order code 
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the owner identifier
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the sales employee identifier 
        /// </summary>
        public string SeId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a customer chose "pick up in store" shipping option
        /// </summary>
        public bool PickUpInStore { get; set; }

        /// <summary>
        /// Gets or sets an order status identifier
        /// </summary>
        public int OrderStatusId { get; set; }

        /// <summary>
        /// Gets or sets the shipping status identifier
        /// </summary>
        public ShippingStatus ShippingStatusId { get; set; }

        /// <summary>
        /// Gets or sets the payment status identifier
        /// </summary>
        public PaymentStatus PaymentStatusId { get; set; }

        /// <summary>
        /// Gets or sets the payment method system name
        /// </summary>
        public string PaymentMethodSystemName { get; set; }

        public string PaymentOptionAttribute { get; set; }

        /// <summary>
        /// Gets or sets the customer currency code (at the moment of order placing)
        /// </summary>
        public string CustomerCurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the primary currency code (at the moment of order placing)
        /// </summary>
        public string PrimaryCurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets the currency rate
        /// </summary>
        public double CurrencyRate { get; set; }

        /// <summary>
        /// Gets or sets the currency rate
        /// </summary>
        public double Rate { get; set; }

        /// <summary>
        /// Gets or sets the customer tax display type identifier
        /// </summary>
        public TaxDisplayType CustomerTaxDisplayTypeId { get; set; }

        /// <summary>
        /// Gets or sets the VAT number 
        /// </summary>
        public string VatNumber { get; set; }

        /// <summary>
        /// Gets or sets the VAT number status id
        /// </summary>
        public int VatNumberStatusId { get; set; }

        /// <summary>
        /// Gets or sets the Company name
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets the CustomerEmail
        /// </summary>
        public string CustomerEmail { get; set; }

        /// <summary>
        /// Gets or sets the FirstName
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the LastName
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the order subtotal (incl tax)
        /// </summary>
        public double OrderSubtotalInclTax { get; set; }

        /// <summary>
        /// Gets or sets the order subtotal (excl tax)
        /// </summary>
        public double OrderSubtotalExclTax { get; set; }

        /// <summary>
        /// Gets or sets the order subtotal discount (incl tax)
        /// </summary>
        public double OrderSubTotalDiscountInclTax { get; set; }

        /// <summary>
        /// Gets or sets the order subtotal discount (excl tax)
        /// </summary>
        public double OrderSubTotalDiscountExclTax { get; set; }

        /// <summary>
        /// Gets or sets the order shipping (incl tax)
        /// </summary>
        public double OrderShippingInclTax { get; set; }

        /// <summary>
        /// Gets or sets the order shipping (excl tax)
        /// </summary>
        public double OrderShippingExclTax { get; set; }

        /// <summary>
        /// Gets or sets the payment method additional fee (incl tax)
        /// </summary>
        public double PaymentMethodAdditionalFeeInclTax { get; set; }

        /// <summary>
        /// Gets or sets the payment method additional fee (excl tax)
        /// </summary>
        public double PaymentMethodAdditionalFeeExclTax { get; set; }

        /// <summary>
        /// Gets or sets the order tax
        /// </summary>
        public double OrderTax { get; set; }

        /// <summary>
        /// Gets or sets the order discount (applied to order total)
        /// </summary>
        public double OrderDiscount { get; set; }

        /// <summary>
        /// Gets or sets the order total
        /// </summary>
        public double OrderTotal { get; set; }

        /// <summary>
        /// Gets or sets the paid amount
        /// </summary>
        public double PaidAmount { get; set; }

        /// <summary>
        /// Gets or sets the paid date and time
        /// </summary>
        public DateTime? PaidDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the refunded amount
        /// </summary>
        public double RefundedAmount { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether loyalty points were earned for this order
        /// </summary>
        public bool LoyaltyPointsWereAdded { get; set; }

        public int RedeemedLoyaltyPoints { get; set; }

        public double RedeemedLoyaltyPointsAmount { get; set; }

        /// Gets or sets the value indicating for calculated loyalty points 
        public int CalcLoyaltyPoints { get; set; }

        /// <summary>
        /// Gets or sets the checkout attribute description
        /// </summary>
        public string CheckoutAttributeDescription { get; set; }

        public IList<CustomAttribute> CheckoutAttributes { get; set; } = new List<CustomAttribute>();

        /// <summary>
        /// Gets or sets the customer language identifier
        /// </summary>
        public string CustomerLanguageId { get; set; }

        /// <summary>
        /// Gets or sets the affiliate identifier
        /// </summary>
        public string AffiliateId { get; set; }

        /// <summary>
        /// Gets or sets the customer IP address
        /// </summary>
        public string CustomerIp { get; set; }

        /// <summary>
        /// Gets or sets the shipping method
        /// </summary>
        public string ShippingMethod { get; set; }

        /// <summary>
        /// Gets or sets the Shipping rate  method identifier
        /// </summary>
        public string ShippingRateProviderSystemName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the date and time of order creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been imported
        /// </summary>
        public bool Imported { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the url referrer exists
        /// </summary>
        public string UrlReferrer { get; set; }

        /// <summary>
        /// Gets or sets the Shipping Option description (customer friendly string)
        /// </summary>
        public string ShippingOptionAttributeDescription { get; set; }

        /// <summary>
        /// Gets or sets the Shipping Option (developer friendly string)
        /// </summary>
        public string ShippingOptionAttribute { get; set; }

        #endregion

        #region Navigation properties

        /// <summary>
        /// Gets or sets the billing address
        /// </summary>
        public virtual Address BillingAddress { get; set; }

        /// <summary>
        /// Gets or sets the shipping address
        /// </summary>
        public virtual Address ShippingAddress { get; set; }

        /// <summary>
        /// Gets or sets the pickup point
        /// </summary>
        public virtual PickupPoint PickupPoint { get; set; }

        /// <summary>
        /// Gets or sets order items
        /// </summary>
        public virtual ICollection<OrderItem> OrderItems {
            get { return _orderItems ??= new List<OrderItem>(); }
            protected set { _orderItems = value; }
        }

        /// <summary>
        /// Gets or sets order taxes
        /// </summary>
        public virtual ICollection<OrderTax> OrderTaxes {
            get { return _orderTaxes ??= new List<OrderTax>(); }
            protected set { _orderTaxes = value; }
        }

        /// <summary>
        /// Gets or sets the order's tags
        /// </summary>
        public virtual ICollection<string> OrderTags {
            get { return _orderTags ??= new List<string>(); }
            protected set { _orderTags = value; }
        }

        #endregion

    }
}
