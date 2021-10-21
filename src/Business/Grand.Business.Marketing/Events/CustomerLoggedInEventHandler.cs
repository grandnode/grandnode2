using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Events
{
    public class CustomerLoggedInEventHandler : INotificationHandler<CustomerLoggedInEvent>
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public CustomerLoggedInEventHandler(
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public Task Handle(CustomerLoggedInEvent notification, CancellationToken cancellationToken)
        {
            //activity log
            _ = _customerActivityService.InsertActivity("PublicStore.Login", notification.Customer.Id, notification.Customer, "", _translationService.GetResource("ActivityLog.PublicStore.Login"));
            return Task.CompletedTask;
        }
    }
}
