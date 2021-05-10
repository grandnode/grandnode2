using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.Messages.Commands.Models
{
    public class GetGiftVoucherTokensCommand : IRequest<LiquidGiftVoucher>
    {
        public GiftVoucher GiftVoucher { get; set; }
        public Language Language { get; set; }
    }
}
