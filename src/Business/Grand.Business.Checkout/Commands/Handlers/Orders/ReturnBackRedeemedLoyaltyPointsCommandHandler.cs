using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class
    ReturnBackRedeemedLoyaltyPointsCommandHandler : IRequestHandler<ReturnBackRedeemedLoyaltyPointsCommand, bool>
{
    private readonly ILoyaltyPointsService _loyaltyPointsService;
    private readonly ILanguageService _languageService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;

    public ReturnBackRedeemedLoyaltyPointsCommandHandler(
        ILoyaltyPointsService loyaltyPointsService,
        ITranslationService translationService,
        ILanguageService languageService,
        IStoreService storeService)
    {
        _loyaltyPointsService = loyaltyPointsService;
        _translationService = translationService;
        _languageService = languageService;
        _storeService = storeService;
    }

    public async Task<bool> Handle(ReturnBackRedeemedLoyaltyPointsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Order);

        //were some points redeemed when placing an order?
        if (request.Order.RedeemedLoyaltyPoints == 0)
            return false;

        //return back
        await _loyaltyPointsService.AddLoyaltyPointsHistory(request.Order.CustomerId,
            -request.Order.RedeemedLoyaltyPoints, request.Order.StoreId,
            string.Format(await GetMessage("LoyaltyPoints.Message.ReturnedForOrder", request.Order),
                request.Order.OrderNumber));

        return true;
    }

    //the message is written in the language the order was placed in, or in the store's language when that is gone
    private async Task<string> GetMessage(string resourceKey, Order order)
    {
        var store = await _storeService.GetStoreById(order.StoreId);
        var language = await _languageService.GetLanguageOrStoreDefault(order.CustomerLanguageId, store);
        return language != null ? _translationService.GetResource(resourceKey, language.Id) : "";
    }
}