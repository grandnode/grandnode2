using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Messages.Tokens;

public class GetAttributeCombinationTokensCommand : IRequest<LiquidAttributeCombination>
{
    public Product Product { get; set; }
    public ProductAttributeCombination Combination { get; set; }
    public Language Language { get; set; }
    public DomainHost Host { get; set; }
}