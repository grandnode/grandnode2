using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetShoppingCartTokensCommand : IRequest<LiquidShoppingCart>
{
    public CalculationContext Context { get; set; }
    public DomainHost Host { get; set; }
    public Language Language { get; set; }
}