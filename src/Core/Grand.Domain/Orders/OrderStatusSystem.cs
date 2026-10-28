namespace Grand.Domain.Orders;

/// <summary>
///     Represents an order status enumeration
/// </summary>
public enum OrderStatusSystem
{
    /// <summary>
    ///     Placed by a company buyer and waiting for an approver's decision
    /// </summary>
    AwaitingApproval = 5,

    /// <summary>
    ///     Pending
    /// </summary>
    Pending = 10,

    /// <summary>
    ///     Processing
    /// </summary>
    Processing = 20,

    /// <summary>
    ///     Complete
    /// </summary>
    Complete = 30,

    /// <summary>
    ///     Cancelled
    /// </summary>
    Cancelled = 40
}