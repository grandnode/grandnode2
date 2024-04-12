using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using MediatR;

namespace Grand.Business.Core.Commands.Messages.Tokens;

public class GetStoreTokensCommand : IRequest<LiquidStore>
{
    public Store Store { get; set; }
    public Language Language { get; set; }
    public EmailAccount EmailAccount { get; set; }
}