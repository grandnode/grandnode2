using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetAuctionTokensCommand : IRequest<LiquidAuctions>
{
    public Product Product { get; set; }
    public Bid Bid { get; set; }
}