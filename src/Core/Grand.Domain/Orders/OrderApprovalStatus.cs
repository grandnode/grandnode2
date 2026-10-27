namespace Grand.Domain.Orders;

/// <summary>
///     Represents the state of an order approval request
/// </summary>
public enum OrderApprovalStatus
{
    /// <summary>
    ///     Waiting for a decision
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     Approved; the order was released
    /// </summary>
    Approved = 10,

    /// <summary>
    ///     Rejected by an approver
    /// </summary>
    Rejected = 20,

    /// <summary>
    ///     Withdrawn by the buyer
    /// </summary>
    Withdrawn = 30
}
