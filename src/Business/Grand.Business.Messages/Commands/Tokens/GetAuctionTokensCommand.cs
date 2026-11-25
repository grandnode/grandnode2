using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetAuctionTokensCommand : IRequest<LiquidAuctions>
{
    public required Product Product { get; set; }
    public required Bid Bid { get; set; }
}