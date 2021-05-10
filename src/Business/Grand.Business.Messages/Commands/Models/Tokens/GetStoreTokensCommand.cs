using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Messages.Commands.Models
{
    public class GetStoreTokensCommand : IRequest<LiquidStore>
    {
        public Store Store { get; set; }
        public Language Language { get; set; }
        public EmailAccount EmailAccount { get; set; }
    }
}
