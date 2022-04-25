using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Events.Customers;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Events.Customers
{
    public class CustomerRegisteredEventHandler : INotificationHandler<CustomerRegisteredEvent>
    {
        private readonly ITranslationService _translationService;
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;

        public CustomerRegisteredEventHandler(
            ITranslationService translationService,
            ILoyaltyPointsService loyaltyPointsService,
            LoyaltyPointsSettings loyaltyPointsSettings
            )
        {
            _translationService = translationService;
            _loyaltyPointsService = loyaltyPointsService;
            _loyaltyPointsSettings = loyaltyPointsSettings;
        }
        public async Task Handle(CustomerRegisteredEvent notification, CancellationToken cancellationToken)
        {
            //Add loyalty points for customer registration (if enabled)
            if (_loyaltyPointsSettings.Enabled &&
                _loyaltyPointsSettings.PointsForRegistration > 0)
            {
                await _loyaltyPointsService.AddLoyaltyPointsHistory(notification.Customer.Id, _loyaltyPointsSettings.PointsForRegistration,
                    notification.Customer.StoreId,
                    _translationService.GetResource("LoyaltyPoints.Message.EarnedForRegistration"), "", 0);
            }
        }
    }
}
