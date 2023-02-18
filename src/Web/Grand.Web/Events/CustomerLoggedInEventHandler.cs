﻿using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using MediatR;
using Microsoft.AspNetCore.Http;

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
