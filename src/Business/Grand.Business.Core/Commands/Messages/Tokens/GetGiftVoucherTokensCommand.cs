using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Commands.Messages.Tokens;

public class GetGiftVoucherTokensCommand : IRequest<LiquidGiftVoucher>
{
    public GiftVoucher GiftVoucher { get; set; }
    public Language Language { get; set; }
}