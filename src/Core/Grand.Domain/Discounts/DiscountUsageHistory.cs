using Grand.Domain.Orders;
using Grand.Domain.Customers;
using Grand.Domain.Audit;

namespace Grand.Domain.Discounts;

/// <summary>
///     Represents a discount usage history entry
/// </summary>
public class DiscountUsageHistory : BaseEntity
{
    /// <summary>
    ///     Gets or sets the discount identifier
    /// </summary>
    [AuditReference(typeof(Discount))]
    public string DiscountId { get; set; }

    /// <summary>
    ///     Gets or sets the discount coupon code
    /// </summary>
    public string CouponCode { get; set; }

    /// <summary>
    ///     Gets or sets the order identifier
    /// </summary>
    [AuditReference(typeof(Order))]
    public string OrderId { get; set; }

    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the canceled identifier
    /// </summary>
    public bool Canceled { get; set; }
}