namespace Grand.Domain.Orders;

/// <summary>
///     Who decides an order approval
/// </summary>
public enum OrderApprovalDecider
{
    /// <summary>
    ///     The buyer's company, on the storefront
    /// </summary>
    Company = 0,

    /// <summary>
    ///     The store, in the back office
    /// </summary>
    Store = 1
}
