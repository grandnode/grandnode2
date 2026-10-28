using Grand.Domain.Configuration;

namespace Grand.Domain.Orders;

/// <summary>
///     Settings of orders that wait for a company approver
/// </summary>
public class OrderApprovalSettings : ISettings
{
    /// <summary>
    ///     Gets or sets the system names of the payment methods offered for orders that need an approval;
    ///     card capture stays away from orders nobody has approved
    /// </summary>
    public List<string> PaymentMethodSystemNames { get; set; } = [];
}
