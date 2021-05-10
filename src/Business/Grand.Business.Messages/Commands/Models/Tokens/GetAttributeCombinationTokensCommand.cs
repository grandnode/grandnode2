using Grand.Domain.Catalog;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Messages.Commands.Models
{
    public class GetAttributeCombinationTokensCommand : IRequest<LiquidAttributeCombination>
    {
        public Product Product { get; set; }
        public ProductAttributeCombination Combination { get; set; }
    }
}
