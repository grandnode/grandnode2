using Grand.Business.Core.Commands.Customers;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using MediatR;
using System.Security.Claims;

namespace Authentication.Facebook.Infrastructure.Cache;

/// <summary>
///     Facebook authentication event consumer (used for saving customer fields on registration)
/// </summary>
public class FacebookAuthenticationEventConsumer : INotificationHandler<RegisteredByExternalMethod>
{
    #region Fields

    private readonly ICustomerService _customerService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IContextAccessor _contextAccessor;
    private readonly CustomerSettings _customerSettings;

    private string WorkingLanguageId => _contextAccessor.WorkContext.WorkingLanguage.Id;
    private Store CurrentStore => _contextAccessor.StoreContext.CurrentStore;

    #endregion

    #region Ctor

    public FacebookAuthenticationEventConsumer(
        ICustomerService customerService,
        IMessageProviderService messageProviderService,
        IContextAccessor contextAccessor,
        CustomerSettings customerSettings)
    {
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _contextAccessor = contextAccessor;
        _customerSettings = customerSettings;
    }

    #endregion

    #region Methods

    public async Task Handle(RegisteredByExternalMethod eventMessage, CancellationToken cancellationToken)
    {
        if (eventMessage?.Customer == null || eventMessage.AuthenticationParameters == null)
            return;

        //handle event only for this authentication method
        if (!eventMessage.AuthenticationParameters.ProviderSystemName.Equals(FacebookAuthenticationDefaults
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
            await _messageProviderService.SendCustomerRegisteredMessage(eventMessage.Customer, CurrentStore, WorkingLanguageId);

        //send welcome message 
        await _messageProviderService.SendCustomerWelcomeMessage(eventMessage.Customer, CurrentStore, WorkingLanguageId);
    }
    #endregion
}