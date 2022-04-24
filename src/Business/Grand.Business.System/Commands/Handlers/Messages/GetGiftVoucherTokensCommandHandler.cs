using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Commands.Messages;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetGiftVoucherTokensCommandHandler : IRequestHandler<GetGiftVoucherTokensCommand, LiquidGiftVoucher>
    {
        private readonly IPriceFormatter _priceFormatter;

        public GetGiftVoucherTokensCommandHandler(IPriceFormatter priceFormatter)
        {
            _priceFormatter = priceFormatter;
        }

        public async Task<LiquidGiftVoucher> Handle(GetGiftVoucherTokensCommand request, CancellationToken cancellationToken)
        {
            var liquidGiftCart = new LiquidGiftVoucher(request.GiftVoucher)
            {
                Amount = await _priceFormatter.FormatPrice(request.GiftVoucher.Amount, request.GiftVoucher.CurrencyCode, false, request.Language)
            };
            return await Task.FromResult(liquidGiftCart);
        }
    }
}
