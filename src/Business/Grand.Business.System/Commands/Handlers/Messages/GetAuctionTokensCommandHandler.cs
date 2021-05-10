using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Messages.Commands.Models;
using Grand.Business.Messages.DotLiquidDrops;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetAuctionTokensCommandHandler : IRequestHandler<GetAuctionTokensCommand, LiquidAuctions>
    {
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IDateTimeService _dateTimeService;

        public GetAuctionTokensCommandHandler(
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            IDateTimeService dateTimeService)
        {
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _dateTimeService = dateTimeService;
        }

        public async Task<LiquidAuctions> Handle(GetAuctionTokensCommand request, CancellationToken cancellationToken)
        {
            var liquidAuctions = new LiquidAuctions(request.Product, request.Bid);
            var defaultCurrency = await _currencyService.GetPrimaryStoreCurrency();
            liquidAuctions.Price = _priceFormatter.FormatPrice(request.Bid.Amount, defaultCurrency);
            liquidAuctions.EndTime = _dateTimeService.ConvertToUserTime(request.Product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc).ToString();
            return liquidAuctions;
        }
    }
}
