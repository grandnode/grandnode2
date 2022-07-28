using Grand.Domain.Catalog;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Core.Commands.Messages
{
    public class GetAttributeCombinationTokensCommand : IRequest<LiquidAttributeCombination>
    {
        public Product Product { get; set; }
        public ProductAttributeCombination Combination { get; set; }
    }
}
