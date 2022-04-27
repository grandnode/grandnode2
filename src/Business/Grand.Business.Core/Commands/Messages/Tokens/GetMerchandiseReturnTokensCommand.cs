using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Core.Commands.Messages
{
    public class GetMerchandiseReturnTokensCommand : IRequest<LiquidMerchandiseReturn>
    {
        public MerchandiseReturn MerchandiseReturn { get; set; }
        public Store Store { get; set; }
        public Order Order { get; set; }
        public DomainHost Host { get; set; }
        public Language Language { get; set; }
        public MerchandiseReturnNote MerchandiseReturnNote { get; set; } = null;
    }
}
