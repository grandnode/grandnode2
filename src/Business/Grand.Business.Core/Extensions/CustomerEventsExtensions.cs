﻿using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Utilities.Customers;
using MediatR;

namespace Grand.Business.Core.Extensions
{
    public static class CustomerEventsExtensions
    {
        public static async Task CustomerRegistrationEvent<C, R>(this IMediator eventPublisher, C result, R request) 
            where C : RegistrationResult where R : RegistrationRequest
        {
            await eventPublisher.Publish(new CustomerRegistrationEvent<C, R>(result, request));
        }
    }
}
