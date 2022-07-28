﻿using Grand.Business.Core.Commands.Customers;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using MediatR;
using System.Security.Claims;

namespace Authentication.Google.Infrastructure.Cache
{
    /// <summary>
    /// Google authentication event consumer (used for saving customer fields on registration)
    /// </summary>
    public partial class GoogleAuthenticationEventConsumer : INotificationHandler<RegisteredByExternalMethod>
    {
        #region Fields

        private readonly IUserFieldService _userFieldService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly IWorkContext _workContext;
        private readonly CustomerSettings _customerSettings;

        #endregion

        #region Ctor

        public GoogleAuthenticationEventConsumer(
            IUserFieldService userFieldService,
            IMessageProviderService messageProviderService,
            IWorkContext workContext,
            CustomerSettings customerSettings
            )
        {
            _userFieldService = userFieldService;
            _messageProviderService = messageProviderService;
            _workContext = workContext;
            _customerSettings = customerSettings;

        }

        #endregion

        #region Methods

        public async Task Handle(RegisteredByExternalMethod eventMessage, CancellationToken cancellationToken)
        {
            if (eventMessage?.Customer == null || eventMessage.AuthenticationParameters == null)
                return;

            //handle event only for this authentication method
            if (!eventMessage.AuthenticationParameters.ProviderSystemName.Equals(GoogleAuthenticationDefaults.ProviderSystemName))
                return;

            //store some of the customer fields
            var firstName = eventMessage.AuthenticationParameters.Claims?.FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName)?.Value;
            if (!string.IsNullOrEmpty(firstName))
                await _userFieldService.SaveField(eventMessage.Customer, SystemCustomerFieldNames.FirstName, firstName);

            var lastName = eventMessage.AuthenticationParameters.Claims?.FirstOrDefault(claim => claim.Type == ClaimTypes.Surname)?.Value;
            if (!string.IsNullOrEmpty(lastName))
                await _userFieldService.SaveField(eventMessage.Customer, SystemCustomerFieldNames.LastName, lastName);

            //notifications for admin
            if (_customerSettings.NotifyNewCustomerRegistration)
                await _messageProviderService.SendCustomerRegisteredMessage(eventMessage.Customer, _workContext.CurrentStore, _workContext.WorkingLanguage.Id);

            //send welcome message 
            if (eventMessage.RegistrationResult.Success)
                await _messageProviderService.SendCustomerWelcomeMessage(eventMessage.Customer, _workContext.CurrentStore, _workContext.WorkingLanguage.Id);

        }

        #endregion
    }
}