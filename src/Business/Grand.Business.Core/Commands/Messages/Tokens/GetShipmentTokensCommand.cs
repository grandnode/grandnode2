using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Core.Commands.Messages
{
    public class GetShipmentTokensCommand : IRequest<LiquidShipment>
    {
        public Shipment Shipment { get; set; }
        public Order Order { get; set; }
        public Store Store { get; set; }
        public DomainHost Host { get; set; }
        public Language Language { get; set; }
    }
}
