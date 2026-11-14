using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Orders;

/// <summary>
///     Sends the link to return items of a guest's order when the number and e-mail address match it; true when an
///     e-mail was queued, which the page never tells
/// </summary>
public class SendGuestMerchandiseReturnLinkCommand : IRequest<bool>
{
    public int OrderNumber { get; set; }
    public string Email { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public DomainHost Host { get; set; }
}
