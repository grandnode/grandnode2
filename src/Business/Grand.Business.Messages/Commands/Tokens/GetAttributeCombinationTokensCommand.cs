using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetAttributeCombinationTokensCommand : IRequest<LiquidAttributeCombination>
{
    public required Product Product { get; set; }
    public required ProductAttributeCombination Combination { get; set; }
    public required Language Language { get; set; }
    public required DomainHost Host { get; set; }
}