using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Messages.Commands.Models
{
    public class GetMerchandiseReturnTokensCommand : IRequest<LiquidMerchandiseReturn>
    {
        public MerchandiseReturn MerchandiseReturn { get; set; }
        public Store Store { get; set; }
        public Order Order { get; set; }
        public Language Language { get; set; }
        public MerchandiseReturnNote MerchandiseReturnNote { get; set; } = null;
    }
}
