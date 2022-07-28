using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Core.Commands.Messages
{
    public class GetGiftVoucherTokensCommand : IRequest<LiquidGiftVoucher>
    {
        public GiftVoucher GiftVoucher { get; set; }
        public Language Language { get; set; }
    }
}
