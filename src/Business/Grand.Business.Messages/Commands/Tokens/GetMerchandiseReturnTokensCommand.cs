using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetMerchandiseReturnTokensCommand : IRequest<LiquidMerchandiseReturn>
{
    public MerchandiseReturn MerchandiseReturn { get; set; }
    public Store Store { get; set; }
    public Order Order { get; set; }
    public DomainHost Host { get; set; }
    public Language Language { get; set; }
    public MerchandiseReturnNote MerchandiseReturnNote { get; set; } = null;
}