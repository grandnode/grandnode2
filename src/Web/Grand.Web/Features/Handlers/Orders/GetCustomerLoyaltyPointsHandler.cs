using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Newsletter
{
    public class GetCustomerLoyaltyPointsHandler : IRequestHandler<GetCustomerLoyaltyPoints, CustomerLoyaltyPointsModel>
    {
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;

        public GetCustomerLoyaltyPointsHandler(ILoyaltyPointsService loyaltyPointsService, IDateTimeService dateTimeService,
            ICurrencyService currencyService, IPriceFormatter priceFormatter, IOrderCalculationService orderTotalCalculationService,
            LoyaltyPointsSettings loyaltyPointsSettings)
        {
            _loyaltyPointsService = loyaltyPointsService;
            _dateTimeService = dateTimeService;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _orderTotalCalculationService = orderTotalCalculationService;
            _loyaltyPointsSettings = loyaltyPointsSettings;
        }

        public async Task<CustomerLoyaltyPointsModel> Handle(GetCustomerLoyaltyPoints request, CancellationToken cancellationToken)
        {
            var model = new CustomerLoyaltyPointsModel();
            foreach (var rph in await _loyaltyPointsService.GetLoyaltyPointsHistory(request.Customer.Id, request.Store.Id))
            {
                model.LoyaltyPoints.Add(new CustomerLoyaltyPointsModel.LoyaltyPointsHistoryModel
                {
                    Points = rph.Points,
                    PointsBalance = rph.PointsBalance,
                    Message = rph.Message,
                    CreatedOn = _dateTimeService.ConvertToUserTime(rph.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
            //current amount/balance
            int loyaltyPointsBalance = await _loyaltyPointsService.GetLoyaltyPointsBalance(request.Customer.Id, request.Store.Id);
            double loyaltyPointsAmountBase = await _orderTotalCalculationService.ConvertLoyaltyPointsToAmount(loyaltyPointsBalance);
            double loyaltyPointsAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(loyaltyPointsAmountBase, request.Currency);
            model.LoyaltyPointsBalance = loyaltyPointsBalance;
            model.LoyaltyPointsAmount = _priceFormatter.FormatPrice(loyaltyPointsAmount, false);
            //minimum amount/balance
            int minimumLoyaltyPointsBalance = _loyaltyPointsSettings.MinimumLoyaltyPointsToUse;
            double minimumLoyaltyPointsAmountBase = await _orderTotalCalculationService.ConvertLoyaltyPointsToAmount(minimumLoyaltyPointsBalance);
            double minimumLoyaltyPointsAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(minimumLoyaltyPointsAmountBase, request.Currency);
            model.MinimumLoyaltyPointsBalance = minimumLoyaltyPointsBalance;
            model.MinimumLoyaltyPointsAmount = _priceFormatter.FormatPrice(minimumLoyaltyPointsAmount, false);

            return model;
        }
    }
}
