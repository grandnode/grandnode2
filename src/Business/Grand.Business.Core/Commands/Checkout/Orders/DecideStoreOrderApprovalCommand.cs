using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     Approves or rejects an order whose approval the store decides. The caller has checked that the customer may
///     manage this order; a request the company decides is refused with false.
/// </summary>
public class DecideStoreOrderApprovalCommand : IRequest<bool>
{
    public Order Order { get; set; }
    public Customer DecidedBy { get; set; }
    public bool Approve { get; set; }
    public string Comment { get; set; }
    public Language Language { get; set; }
}
