using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using MediatR;

namespace Grand.Business.Core.Commands.Messages.Tokens;

public class GetMerchandiseReturnTokensCommand : IRequest<LiquidMerchandiseReturn>
{
    public MerchandiseReturn MerchandiseReturn { get; set; }
    public Store Store { get; set; }
    public Order Order { get; set; }
    public DomainHost Host { get; set; }
    public Language Language { get; set; }
    public MerchandiseReturnNote MerchandiseReturnNote { get; set; } = null;
}