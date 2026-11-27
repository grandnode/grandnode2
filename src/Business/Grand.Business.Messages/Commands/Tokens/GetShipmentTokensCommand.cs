using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetShipmentTokensCommand : IRequest<LiquidShipment>
{
    public required Shipment Shipment { get; set; }
    public required Order Order { get; set; }
    public required Store Store { get; set; }
    public required DomainHost Host { get; set; }
    public required Language Language { get; set; }
}