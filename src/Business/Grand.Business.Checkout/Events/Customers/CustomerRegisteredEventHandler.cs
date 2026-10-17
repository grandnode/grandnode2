using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Customers;

public class CustomerRegisteredEventHandler : INotificationHandler<CustomerRegisteredEvent>
{
    private readonly ILanguageService _languageService;
    private readonly ILoyaltyPointsService _loyaltyPointsService;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;

    public CustomerRegisteredEventHandler(
        ITranslationService translationService,
        ILoyaltyPointsService loyaltyPointsService,
        LoyaltyPointsSettings loyaltyPointsSettings,
        IStoreService storeService,
        ILanguageService languageService
    )
    {
        _translationService = translationService;
        _loyaltyPointsService = loyaltyPointsService;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _storeService = storeService;
        _languageService = languageService;
    }

    public async Task Handle(CustomerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        //Add loyalty points for customer registration (if enabled)
        if (!_loyaltyPointsSettings.Enabled || _loyaltyPointsSettings.PointsForRegistration <= 0)
            return;

        //the request language; registration without a request (access-token sign-up) writes in the store's language
        var language = notification.Language ??
                       await _languageService.GetLanguageOrStoreDefault(null,
                           await _storeService.GetStoreById(notification.Customer.StoreId));
        await _loyaltyPointsService.AddLoyaltyPointsHistory(notification.Customer.Id,
            _loyaltyPointsSettings.PointsForRegistration,
            notification.Customer.StoreId,
            language != null
                ? _translationService.GetResource("LoyaltyPoints.Message.EarnedForRegistration", language.Id)
                : "");
    }
}