using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Commands.Messages.Tokens;

public class GetAuctionTokensCommand : IRequest<LiquidAuctions>
{
    public Product Product { get; set; }
    public Bid Bid { get; set; }
}