﻿using Grand.Business.Core.Commands.Customers;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using MediatR;
using System.Security.Claims;

namespace Authentication.Google.Infrastructure;

/// <summary>
///     Google authentication event consumer (used for saving customer fields on registration)
/// </summary>
public class GoogleAuthenticationEventConsumer : INotificationHandler<RegisteredByExternalMethod>
{
    #region Ctor

    public GoogleAuthenticationEventConsumer(
        ICustomerService customerService,
        IMessageProviderService messageProviderService,
        IWorkContextAccessor workContextAccessor,
        CustomerSettings customerSettings
    )
    {
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _workContextAccessor = workContextAccessor;
        _customerSettings = customerSettings;
    }

    #endregion

    #region Methods

    public async Task Handle(RegisteredByExternalMethod eventMessage, CancellationToken cancellationToken)
    {
        if (eventMessage?.Customer == null || eventMessage.AuthenticationParameters == null)
            return;

        //handle event only for this authentication method
        if (!eventMessage.AuthenticationParameters.ProviderSystemName.Equals(GoogleAuthenticationDefaults
                .ProviderSystemName))
            return;

        //store some of the customer fields
        var firstName = eventMessage.AuthenticationParameters.Claims
            ?.FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName)?.Value;
        if (!string.IsNullOrEmpty(firstName))
            await _customerService.UpdateUserField(eventMessage.Customer, SystemCustomerFieldNames.FirstName, firstName);

        var lastName = eventMessage.AuthenticationParameters.Claims
            ?.FirstOrDefault(claim => claim.Type == ClaimTypes.Surname)?.Value;
        if (!string.IsNullOrEmpty(lastName))
            await _customerService.UpdateUserField(eventMessage.Customer, SystemCustomerFieldNames.LastName, lastName);

        //notifications for admin
        if (_customerSettings.NotifyNewCustomerRegistration)
            await _messageProviderService.SendCustomerRegisteredMessage(eventMessage.Customer,
                _workContextAccessor.WorkContext.CurrentStore, _workContextAccessor.WorkContext.WorkingLanguage.Id);

        //send welcome message 
        await _messageProviderService.SendCustomerWelcomeMessage(eventMessage.Customer, _workContextAccessor.WorkContext.CurrentStore,
            _workContextAccessor.WorkContext.WorkingLanguage.Id);
    }

    #endregion

    #region Fields

    private readonly ICustomerService _customerService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly CustomerSettings _customerSettings;

    #endregion
}