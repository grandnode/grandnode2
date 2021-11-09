using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Events;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Events
{
    public class CustomerLoggedInEventHandler : INotificationHandler<CustomerLoggedInEvent>
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITranslationService _translationService;

        public CustomerLoggedInEventHandler(
            ICustomerActivityService customerActivityService,
            IHttpContextAccessor httpContextAccessor,
            ITranslationService translationService)
        {
            _customerActivityService = customerActivityService;
            _httpContextAccessor = httpContextAccessor;
            _translationService = translationService;
        }

        public Task Handle(CustomerLoggedInEvent notification, CancellationToken cancellationToken)
        {
            //activity log
            _ = _customerActivityService.InsertActivity("PublicStore.Login", 
                notification.Customer.Id, notification.Customer,
                _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.PublicStore.Login"));
            return Task.CompletedTask;
        }
    }
}
