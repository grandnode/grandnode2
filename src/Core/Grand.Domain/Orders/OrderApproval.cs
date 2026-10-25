namespace Grand.Domain.Orders;

/// <summary>
///     Represents a request to approve a company sub-account's cart before the order is placed
/// </summary>
public class OrderApproval : BaseEntity
{
    /// <summary>
    ///     Gets or sets the store identifier the cart belongs to
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the company owner; every read is scoped by it
    /// </summary>
    public string CompanyOwnerId { get; set; }

    /// <summary>
    ///     Gets or sets the buyer identifier
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the status
    /// </summary>
    public OrderApprovalStatus Status { get; set; }

    /// <summary>
    ///     Gets or sets the hash of the approved cart
    /// </summary>
    public string CartFingerprint { get; set; }

    /// <summary>
    ///     Gets or sets the cart lines as the approver sees them
    /// </summary>
    public IList<OrderApprovalItem> Items { get; set; } = new List<OrderApprovalItem>();

    /// <summary>
    ///     Gets or sets the order totals as the buyer saw them
    /// </summary>
    public OrderApprovalSummary Summary { get; set; } = new();

    /// <summary>
    ///     Gets or sets the order total compared with the limit, in the primary store currency
    /// </summary>
    public double Amount { get; set; }

    /// <summary>
    ///     Gets or sets the buyer's currency code, for display
    /// </summary>
    public string CurrencyCode { get; set; }

    /// <summary>
    ///     Gets or sets the date and time of the decision
    /// </summary>
    public DateTime? DecidedOnUtc { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the customer who decided
    /// </summary>
    public string DecidedByCustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the approver's comment
    /// </summary>
    public string DecisionComment { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the order that used the approval
    /// </summary>
    public string OrderId { get; set; }
}
