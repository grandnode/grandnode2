using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Orders;

/// <summary>
///     Sends the link that opens a guest's order when its number or code and the e-mail address match it; true when an
///     e-mail was queued, which the page never tells
/// </summary>
public class SendGuestOrderLinkCommand : IRequest<bool>
{
    public string OrderNumberOrCode { get; set; }
    public string Email { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public DomainHost Host { get; set; }
}
