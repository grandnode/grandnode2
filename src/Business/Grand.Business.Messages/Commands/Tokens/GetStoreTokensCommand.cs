using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetStoreTokensCommand : IRequest<LiquidStore>
{
    public required Store Store { get; set; }
    public required Language Language { get; set; }
    public required EmailAccount EmailAccount { get; set; }
}