using System;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents a gift voucher usage history entry
    /// </summary>
    public partial class GiftVoucherUsageHistory : SubBaseEntity
    {
        /// <summary>
        /// Gets or sets the gift voucher identifier
        /// </summary>
        public string GiftVoucherId { get; set; }

        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        public string UsedWithOrderId { get; set; }

        /// <summary>
        /// Gets or sets the used value (amount)
        /// </summary>
        public double UsedValue { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
        
    }
}
