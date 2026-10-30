using Grand.Domain.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Audit;

namespace Grand.Domain.Orders;

/// <summary>
///     Represents a loyalty point history entry
/// </summary>
public class LoyaltyPointsHistory : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the store identifier in which these loyalty points were awarded or redeemed
    /// </summary>
    [AuditReference(typeof(Store))]
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets the points redeemed/added
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    ///     Gets or sets the points balance
    /// </summary>
    public int PointsBalance { get; set; }

    /// <summary>
    ///     Gets or sets the used amount
    /// </summary>
    public double UsedAmount { get; set; }

    /// <summary>
    ///     Gets or sets the message
    /// </summary>
    public string Message { get; set; }

    [AuditReference(typeof(Order))]
    public string UsedWithOrderId { get; set; }
}