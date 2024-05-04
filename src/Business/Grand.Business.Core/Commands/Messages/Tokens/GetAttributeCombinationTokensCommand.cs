using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Core.Commands.Messages.Tokens;

public class GetAttributeCombinationTokensCommand : IRequest<LiquidAttributeCombination>
{
    public Product Product { get; set; }
    public ProductAttributeCombination Combination { get; set; }
}