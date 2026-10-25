using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Orders;

/// <summary>
///     Withdraws the buyer's own pending or approved order approval
/// </summary>
public class WithdrawOrderApprovalCommand : IRequest<bool>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public string ApprovalId { get; set; }
}
