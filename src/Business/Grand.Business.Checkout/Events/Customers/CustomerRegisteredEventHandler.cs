using Grand.Business.Core.Events.Customers;
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

        var languageId = notification.Language?.Id ?? await GetStoreLanguageId(notification.Customer.StoreId);
        await _loyaltyPointsService.AddLoyaltyPointsHistory(notification.Customer.Id,
            _loyaltyPointsSettings.PointsForRegistration,
            notification.Customer.StoreId,
            _translationService.GetResource("LoyaltyPoints.Message.EarnedForRegistration", languageId));
    }

    private async Task<string> GetStoreLanguageId(string storeId)
    {
        var store = await _storeService.GetStoreById(storeId);
        if (!string.IsNullOrEmpty(store?.DefaultLanguageId))
            return store.DefaultLanguageId;

        return (await _languageService.GetAllLanguages(storeId: storeId)).FirstOrDefault()?.Id;
    }
}