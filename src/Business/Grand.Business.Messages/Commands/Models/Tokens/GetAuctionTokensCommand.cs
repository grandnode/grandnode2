using Grand.Domain.Catalog;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Messages.Commands.Models
{
    public class GetAuctionTokensCommand : IRequest<LiquidAuctions>
    {
        public Product Product { get; set; }
        public Bid Bid { get; set; }
    }
}
