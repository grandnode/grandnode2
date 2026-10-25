using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Orders;

/// <summary>
///     Asks the company's approvers to approve the buyer's current cart. Returns the refusal as a translated
///     message when the cart needs no approval.
/// </summary>
public class SubmitOrderApprovalCommand : IRequest<(bool success, string error)>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Language Language { get; set; }
    public CalculationContext Context { get; set; }
    public DomainHost Host { get; set; }
}
