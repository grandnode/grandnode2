using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     Applies a decision to an order approval. The caller has checked that the customer may decide it; this command
///     refuses, with false, a request that is no longer open and a customer deciding their own order.
/// </summary>
public class ApplyOrderApprovalDecisionCommand : IRequest<bool>
{
    public OrderApproval Approval { get; set; }
    public Customer DecidedBy { get; set; }
    public bool Approve { get; set; }
    public string Comment { get; set; }
    public Language Language { get; set; }

    /// <summary>
    ///     Host of the storefront request; empty in a back office panel, links then use the store's primary domain
    /// </summary>
    public DomainHost Host { get; set; }
}
