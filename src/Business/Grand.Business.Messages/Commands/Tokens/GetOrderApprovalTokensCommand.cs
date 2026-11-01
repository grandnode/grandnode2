using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetOrderApprovalTokensCommand : IRequest<LiquidOrderApproval>
{
    public OrderApproval Approval { get; set; }
    public Customer Buyer { get; set; }
    public Store Store { get; set; }
    public DomainHost Host { get; set; }
    public Language Language { get; set; }

    /// <summary>
    ///     The back office panel the recipient works in; empty for a message to the buyer
    /// </summary>
    public string PanelArea { get; set; }
}
