using Grand.Domain.Catalog;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents a gift voucher
    /// </summary>
    public partial class GiftVoucher : BaseEntity
    {
        private ICollection<GiftVoucherUsageHistory> _giftVoucherUsageHistory;
        
        /// <summary>
        /// Gets or sets the gift voucher type identifier
        /// </summary>
        public GiftVoucherType GiftVoucherTypeId { get; set; }

        /// <summary>
        /// Gets or sets the amount
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets the currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gift voucher is activated
        /// </summary>
        public bool IsGiftVoucherActivated { get; set; }

        /// <summary>
        /// Gets or sets a gift voucher coupon code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets a recipient name
        /// </summary>
        public string RecipientName { get; set; }

        /// <summary>
        /// Gets or sets a recipient email
        /// </summary>
        public string RecipientEmail { get; set; }

        /// <summary>
        /// Gets or sets a sender name
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// Gets or sets a sender email
        /// </summary>
        public string SenderEmail { get; set; }

        /// <summary>
        /// Gets or sets a message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether recipient is notified
        /// </summary>
        public bool IsRecipientNotified { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
        
        /// <summary>
        /// Gets or sets the gift voucher usage history
        /// </summary>
        public virtual ICollection<GiftVoucherUsageHistory> GiftVoucherUsageHistory
        {
            get { return _giftVoucherUsageHistory ??= new List<GiftVoucherUsageHistory>(); }
            protected set { _giftVoucherUsageHistory = value; }
        }
        
        /// <summary>
        /// Gets or sets the associated order item
        /// </summary>
        public virtual OrderItem PurchasedWithOrderItem { get; set; }
    }
}
