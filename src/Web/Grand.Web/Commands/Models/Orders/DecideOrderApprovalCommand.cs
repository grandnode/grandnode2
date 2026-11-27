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
    public required Customer Approver { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required string ApprovalId { get; set; }
    public bool Approve { get; set; }
    public string? Comment { get; set; }
    public required Language Language { get; set; }
    public required DomainHost Host { get; set; }
}
