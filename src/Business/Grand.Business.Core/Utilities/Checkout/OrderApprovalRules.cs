using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Who belongs to which company and who may decide its order approvals
/// </summary>
public static class OrderApprovalRules
{
    /// <summary>
    ///     Gets the identifier of the company the customer buys for: the owner's id for a sub-account,
    ///     the customer's own id otherwise
    /// </summary>
    public static string CompanyId(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return string.IsNullOrEmpty(customer.OwnerId) ? customer.Id : customer.OwnerId;
    }

    /// <summary>
    ///     Gets a value indicating whether the customer may decide the company's approvals: the company owner
    ///     or an approver sub-account
    /// </summary>
    public static bool CanApprove(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return string.IsNullOrEmpty(customer.OwnerId) || customer.SubAccountRole == SubAccountRole.Approver;
    }
}
