using Grand.Domain.Audit;

namespace Grand.Domain.Orders;

/// <summary>
///     Represents a gift voucher usage history entry
/// </summary>
public class GiftVoucherUsageHistory : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the gift voucher identifier
    /// </summary>
    [AuditReference(typeof(GiftVoucher))]
    public string GiftVoucherId { get; set; }

    /// <summary>
    ///     Gets or sets the order identifier
    /// </summary>
    [AuditReference(typeof(Order))]
    public string UsedWithOrderId { get; set; }

    /// <summary>
    ///     Gets or sets the used value (amount)
    /// </summary>
    public double UsedValue { get; set; }

    /// <summary>
    ///     Gets or sets the date and time of instance creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }
}