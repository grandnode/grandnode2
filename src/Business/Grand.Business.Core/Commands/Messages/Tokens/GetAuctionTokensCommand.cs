using Grand.Domain.Catalog;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Core.Commands.Messages
{
    public class GetAuctionTokensCommand : IRequest<LiquidAuctions>
    {
        public Product Product { get; set; }
        public Bid Bid { get; set; }
    }
}
