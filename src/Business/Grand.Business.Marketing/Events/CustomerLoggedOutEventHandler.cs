using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Events
{
    public class CustomerLoggedOutEventHandler : INotificationHandler<CustomerLoggedOutEvent>
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public CustomerLoggedOutEventHandler(
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task Handle(CustomerLoggedOutEvent notification, CancellationToken cancellationToken)
        {
            //activity log
            await _customerActivityService.InsertActivity("PublicStore.Logout", "", _translationService.GetResource("ActivityLog.PublicStore.Logout"), notification.Customer);
        }
    }
}
