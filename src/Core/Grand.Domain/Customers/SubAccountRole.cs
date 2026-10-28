namespace Grand.Domain.Customers;

/// <summary>
///     Purchasing role of a company sub-account
/// </summary>
public enum SubAccountRole
{
    /// <summary>
    ///     Orders above the account's order limit need an approval
    /// </summary>
    Buyer = 0,

    /// <summary>
    ///     Approves the company's order requests and orders without a limit
    /// </summary>
    Approver = 10
}
