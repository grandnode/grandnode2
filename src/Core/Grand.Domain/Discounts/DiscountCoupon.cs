using Grand.Domain.Audit;

namespace Grand.Domain.Discounts;

public class DiscountCoupon : BaseEntity
{
    /// <summary>
    ///     Gets or sets the coupon code
    /// </summary>
    [AuditName]
    public string CouponCode { get; set; }

    /// <summary>
    ///     Gets or sets the discount id
    /// </summary>
    [AuditReference(typeof(Discount))]
    public string DiscountId { get; set; }

    /// <summary>
    ///     Gets or sets discount used
    /// </summary>
    public bool Used { get; set; }

    /// <summary>
    ///     How many times was used
    /// </summary>
    public int Qty { get; set; }
}