using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetMerchandiseReturnTokensCommand : IRequest<LiquidMerchandiseReturn>
{
    public required MerchandiseReturn MerchandiseReturn { get; set; }
    public required Store Store { get; set; }
    public required Order Order { get; set; }
    public required DomainHost Host { get; set; }
    public required Language Language { get; set; }
    public MerchandiseReturnNote? MerchandiseReturnNote { get; set; } = null;

    //the note shown to the customer with the status change, e.g. why the return was rejected
    public string? StatusNote { get; set; }
}