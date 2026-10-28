using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Orders;

/// <summary>
///     Approves or rejects a pending order approval of the approver's company. Returns false, without any
///     detail, when the approver may not decide it.
/// </summary>
public class DecideOrderApprovalCommand : IRequest<bool>
{
    public Customer Approver { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public string ApprovalId { get; set; }
    public bool Approve { get; set; }
    public string Comment { get; set; }
    public Language Language { get; set; }
    public DomainHost Host { get; set; }
}
