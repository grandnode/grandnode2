using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Handlers.Orders;

public class GetCustomerLoyaltyPointsHandler : IRequestHandler<GetCustomerLoyaltyPoints, CustomerLoyaltyPointsModel>
{
    private readonly ICurrencyService _currencyService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILoyaltyPointsService _loyaltyPointsService;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly IOrderCalculationService _orderTotalCalculationService;
    private readonly IPriceFormatter _priceFormatter;

    public GetCustomerLoyaltyPointsHandler(ILoyaltyPointsService loyaltyPointsService, IDateTimeService dateTimeService,
        ICurrencyService currencyService, IPriceFormatter priceFormatter,
        IOrderCalculationService orderTotalCalculationService,
        LoyaltyPointsSettings loyaltyPointsSettings)
    {
        _loyaltyPointsService = loyaltyPointsService;
        _dateTimeService = dateTimeService;
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        _orderTotalCalculationService = orderTotalCalculationService;
        _loyaltyPointsSettings = loyaltyPointsSettings;
    }

    public async Task<CustomerLoyaltyPointsModel> Handle(GetCustomerLoyaltyPoints request,
        CancellationToken cancellationToken)
    {
        var model = new CustomerLoyaltyPointsModel();
        foreach (var rph in await _loyaltyPointsService.GetLoyaltyPointsHistory(request.Customer.Id, request.Store.Id))
            model.LoyaltyPoints.Add(new CustomerLoyaltyPointsModel.LoyaltyPointsHistoryModel {
                Points = rph.Points,
                PointsBalance = rph.PointsBalance,
                Message = rph.Message,
                CreatedOn = _dateTimeService.ConvertToUserTime(rph.CreatedOnUtc, DateTimeKind.Utc)
            });
        //current amount/balance
        var loyaltyPointsBalance =
            await _loyaltyPointsService.GetLoyaltyPointsBalance(request.Customer.Id, request.Store.Id);
        var loyaltyPointsAmountBase =
            await _orderTotalCalculationService.ConvertLoyaltyPointsToAmount(loyaltyPointsBalance);
        var loyaltyPointsAmount =
            await _currencyService.ConvertFromPrimaryStoreCurrency(loyaltyPointsAmountBase, request.Currency);
        model.LoyaltyPointsBalance = loyaltyPointsBalance;
        model.LoyaltyPointsAmount = _priceFormatter.FormatPrice(loyaltyPointsAmount, request.Currency);
        //minimum amount/balance
        var minimumLoyaltyPointsBalance = _loyaltyPointsSettings.MinimumLoyaltyPointsToUse;
        var minimumLoyaltyPointsAmountBase =
            await _orderTotalCalculationService.ConvertLoyaltyPointsToAmount(minimumLoyaltyPointsBalance);
        var minimumLoyaltyPointsAmount =
            await _currencyService.ConvertFromPrimaryStoreCurrency(minimumLoyaltyPointsAmountBase, request.Currency);
        model.MinimumLoyaltyPointsBalance = minimumLoyaltyPointsBalance;
        model.MinimumLoyaltyPointsAmount = _priceFormatter.FormatPrice(minimumLoyaltyPointsAmount, request.Currency);

        return model;
    }
}