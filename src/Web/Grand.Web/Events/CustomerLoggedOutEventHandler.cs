using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Events.Customers;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Events
{
    public class CustomerLoggedOutEventHandler : INotificationHandler<CustomerLoggedOutEvent>
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITranslationService _translationService;

        public CustomerLoggedOutEventHandler(
            ICustomerActivityService customerActivityService,
            IHttpContextAccessor httpContextAccessor,
            ITranslationService translationService)
        {
            _customerActivityService = customerActivityService;
            _httpContextAccessor = httpContextAccessor;
            _translationService = translationService;
        }

        public Task Handle(CustomerLoggedOutEvent notification, CancellationToken cancellationToken)
        {
            //activity log
            _ = _customerActivityService.InsertActivity("PublicStore.Logout", "", notification.Customer,
                _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.PublicStore.Logout"));
            return Task.CompletedTask;
        }
    }
}
