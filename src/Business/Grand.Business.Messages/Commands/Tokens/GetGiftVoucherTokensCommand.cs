using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetGiftVoucherTokensCommand : IRequest<LiquidGiftVoucher>
{
    public GiftVoucher GiftVoucher { get; set; }
    public Language Language { get; set; }
}