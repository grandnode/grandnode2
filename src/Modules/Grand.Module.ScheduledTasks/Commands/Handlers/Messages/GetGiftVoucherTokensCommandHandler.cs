using Grand.Business.Core.Commands.Messages.Tokens;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using MediatR;

namespace Grand.Business.System.Commands.Handlers.Messages;

public class GetGiftVoucherTokensCommandHandler : IRequestHandler<GetGiftVoucherTokensCommand, LiquidGiftVoucher>
{
    private readonly ICurrencyService _currencyService;
    private readonly IPriceFormatter _priceFormatter;

    public GetGiftVoucherTokensCommandHandler(IPriceFormatter priceFormatter, ICurrencyService currencyService)
    {
        _priceFormatter = priceFormatter;
        _currencyService = currencyService;
    }

    public async Task<LiquidGiftVoucher> Handle(GetGiftVoucherTokensCommand request,
        CancellationToken cancellationToken)
    {
        var liquidGiftCart = new LiquidGiftVoucher(request.GiftVoucher) {
            Amount = _priceFormatter.FormatPrice(request.GiftVoucher.Amount,
                await _currencyService.GetCurrencyByCode(request.GiftVoucher.CurrencyCode))
        };
        return await Task.FromResult(liquidGiftCart);
    }
}