using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Messages.Tokens;

public class GetShoppingCartTokensCommand : IRequest<LiquidShoppingCart>
{
    public CalculationContext Context { get; set; }
    public DomainHost Host { get; set; }
    public Language Language { get; set; }
}