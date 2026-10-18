using Grand.Business.Core.Commands.Customers;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using Grand.Mediator;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Determines the external authentication service implementation
/// </summary>
public class ExternalAuthenticationService : IExternalAuthenticationService
{
    #region Ctor

    public ExternalAuthenticationService(
        ICustomerManagerService customerManagerService,
        ICustomerService customerService,
        IGroupService groupService,
        IMediator mediator,
        IRepository<ExternalAuthentication> externalAuthenticationRecordRepository,
        IEnumerable<IExternalAuthenticationProvider> externalAuthenticationProviders,
        CustomerSettings customerSettings,
        ExternalAuthenticationSettings externalAuthenticationSettings)
    {
        _customerSettings = customerSettings;
        _externalAuthenticationSettings = externalAuthenticationSettings;
        _customerManagerService = customerManagerService;
        _customerService = customerService;
        _groupService = groupService;
        _mediator = mediator;
        _externalAuthenticationRecordRepository = externalAuthenticationRecordRepository;
        _externalAuthenticationProviders = externalAuthenticationProviders;
    }

    #endregion

    #region Fields

    private readonly ICustomerManagerService _customerManagerService;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IMediator _mediator;
    private readonly IRepository<ExternalAuthentication> _externalAuthenticationRecordRepository;
    private readonly IEnumerable<IExternalAuthenticationProvider> _externalAuthenticationProviders;
    private readonly CustomerSettings _customerSettings;
    private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;

    #endregion

    #region Utilities

    /// <summary>
    ///     Authenticate user with existing associated external account
    /// </summary>
    /// <param name="associatedUser">Associated with passed external authentication parameters user</param>
    /// <param name="currentLoggedInUser">Current logged-in user</param>
    /// <param name="customer">Customer of the request (a guest when nobody is logged in)</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="language">Working language of the request</param>
    /// <returns>Result of an authentication</returns>
    protected virtual async Task<ExternalAuthResult> AuthenticateExistingUser(Customer associatedUser,
        Customer currentLoggedInUser, Customer customer, string storeId, Language language)
    {
        //log in guest user
        if (currentLoggedInUser == null)
            return await LoginUser(associatedUser, customer, storeId, language);

        //account is already assigned to another user
        if (currentLoggedInUser.Id != associatedUser.Id)
            return ExternalAuthResult.AlreadyAssigned();

        return ExternalAuthResult.Associated(currentLoggedInUser);
    }

    /// <summary>
    ///     Authenticate current user and associate new external account with user
    /// </summary>
    /// <param name="currentLoggedInUser">Current logged-in user</param>
    /// <param name="parameters">Authentication parameters received from external authentication method</param>
    /// <param name="customer">Customer of the request (a guest when nobody is logged in)</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="language">Working language of the request</param>
    /// <returns>Result of an authentication</returns>
    protected virtual async Task<ExternalAuthResult> AuthenticateNewUser(Customer currentLoggedInUser,
        ExternalAuthParam parameters, Customer customer, string storeId, Language language)
    {
        //associate external account with logged-in user
        if (currentLoggedInUser != null)
        {
            await AssociateCustomer(currentLoggedInUser, parameters);
            return ExternalAuthResult.Associated(currentLoggedInUser);
        }

        //or try to register new user
        if (_customerSettings.UserRegistrationType != UserRegistrationType.Disabled)
            return await RegisterNewUser(parameters, customer, storeId, language);

        //registration is disabled
        return ExternalAuthResult.RegistrationDisabled();
    }

    /// <summary>
    ///     Register new user
    /// </summary>
    /// <param name="parameters">Authentication parameters received from external authentication method</param>
    /// <param name="customer">Guest customer of the request; registration turns it into the new user</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="language">Working language of the request</param>
    /// <returns>Result of an authentication</returns>
    protected virtual async Task<ExternalAuthResult> RegisterNewUser(ExternalAuthParam parameters, Customer customer,
        string storeId, Language language)
    {
        var approved =
            _customerSettings.UserRegistrationType is UserRegistrationType.Standard
                or UserRegistrationType.EmailValidation;

        //create registration request
        var registrationRequest = new RegistrationRequest(customer,
            parameters.Email, parameters.Email,
            CommonHelper.GenerateRandomDigitCode(20),
            PasswordFormat.Hashed,
            storeId,
            approved);

        //whether registration request has been completed successfully
        await _customerManagerService.RegisterCustomer(registrationRequest);

        //allow to save other customer values by consuming this event
        await _mediator.Publish(new RegisteredByExternalMethod(customer, parameters));

        //raise customer registered event
        await _mediator.Publish(new CustomerRegisteredEvent(customer, language));

        //associate external account with registered user
        await AssociateCustomer(customer, parameters);

        //Disabled never gets here, so the only unapproved registration type is AdminApproval
        return approved ? ExternalAuthResult.Registered(customer) : ExternalAuthResult.AwaitingApproval(customer);
    }

    /// <summary>
    ///     Login passed user
    /// </summary>
    /// <param name="user">User to login</param>
    /// <param name="previousCustomer">Customer of the request before the login (the guest whose cart is merged)</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="language">Working language of the request</param>
    /// <returns>Result of an authentication</returns>
    protected virtual async Task<ExternalAuthResult> LoginUser(Customer user, Customer previousCustomer,
        string storeId, Language language)
    {
        //raise event; the caller signs the user in afterwards
        await _mediator.Publish(new CustomerLoggedInEvent(user, previousCustomer, storeId, language));

        return ExternalAuthResult.LoggedIn(user);
    }

    #endregion

    #region Methods

    #region External authentication methods

    /// <summary>
    ///     Load active external authentication providers
    /// </summary>
    /// <returns>External Authentication Providers</returns>
    public virtual IList<IExternalAuthenticationProvider> LoadActiveAuthenticationProviders(Customer customer,
        Store store)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(store);

        return LoadAllAuthenticationProviders()
            .Where(provider =>
                provider.IsMethodActive(_externalAuthenticationSettings) &&
                provider.IsAuthenticateGroup(customer) &&
                provider.IsAuthenticateStore(store)
            ).ToList();
    }

    /// <summary>
    ///     Load external authentication provider by system name
    /// </summary>
    /// <param name="systemName">System name</param>
    /// <returns>Found external authentication provider</returns>
    public virtual IExternalAuthenticationProvider LoadAuthenticationProviderBySystemName(string systemName)
    {
        return _externalAuthenticationProviders.FirstOrDefault(x =>
            x.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Load all external authentication providers
    /// </summary>
    /// <returns>External authentication methods</returns>
    public virtual IList<IExternalAuthenticationProvider> LoadAllAuthenticationProviders()
    {
        return _externalAuthenticationProviders.OrderBy(x => x.Priority).ToList();
    }

    /// <summary>
    ///     Check whether authentication by the passed external authentication provider is available
    /// </summary>
    /// <param name="systemName">System name of the external authentication provider</param>
    /// <param name="customer">Customer whose groups the provider may be limited to</param>
    /// <param name="store">Store the provider may be limited to</param>
    /// <returns>True if is available; otherwise false</returns>
    public virtual bool AuthenticationProviderIsAvailable(string systemName, Customer customer, Store store)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(store);

        var authenticationMethod = LoadAuthenticationProviderBySystemName(systemName);

        return authenticationMethod != null &&
               authenticationMethod.IsMethodActive(_externalAuthenticationSettings) &&
               authenticationMethod.IsAuthenticateGroup(customer) &&
               authenticationMethod.IsAuthenticateStore(store);
    }

    #endregion

    #region Authentication

    /// <summary>
    ///     Authenticate user by passed parameters
    /// </summary>
    /// <param name="parameters">External authentication parameters</param>
    /// <param name="customer">Customer of the request (a guest when nobody is logged in)</param>
    /// <param name="store">Store of the request</param>
    /// <param name="language">Working language of the request</param>
    /// <returns>Result of an authentication</returns>
    public virtual async Task<ExternalAuthResult> Authenticate(ExternalAuthParam parameters, Customer customer,
        Store store, Language language)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(language);

        if (!AuthenticationProviderIsAvailable(parameters.ProviderSystemName, customer, store))
            return ExternalAuthResult.Error("External authentication method cannot be loaded");

        //get current logged-in user
        var currentLoggedInUser = await _groupService.IsRegistered(customer) ? customer : null;

        //authenticate associated user if already exists
        var associatedUser = await GetCustomer(parameters);
        if (associatedUser != null)
            return await AuthenticateExistingUser(associatedUser, currentLoggedInUser, customer, store.Id, language);

        //or associate and authenticate new user
        return await AuthenticateNewUser(currentLoggedInUser, parameters, customer, store.Id, language);
    }

    #endregion

    /// <summary>
    ///     Associate external account with customer
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="parameters">External authentication parameters</param>
    public virtual async Task AssociateCustomer(Customer customer, ExternalAuthParam parameters)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var externalAuthenticationRecord = new ExternalAuthentication {
            CustomerId = customer.Id,
            Email = parameters.Email,
            ExternalIdentifier = parameters.Identifier,
            ExternalDisplayIdentifier = parameters.Name,
            OAuthAccessToken = parameters.AccessToken,
            ProviderSystemName = parameters.ProviderSystemName
        };

        await _externalAuthenticationRecordRepository.InsertAsync(externalAuthenticationRecord);
    }

    /// <summary>
    ///     Get the particular user with specified parameters
    /// </summary>
    /// <param name="parameters">External authentication parameters</param>
    /// <returns>Customer</returns>
    public virtual async Task<Customer> GetCustomer(ExternalAuthParam parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var associationRecord = (from q in _externalAuthenticationRecordRepository.Table
            where q.ExternalIdentifier == parameters.Identifier
                  && q.ProviderSystemName.ToLowerInvariant() == parameters.ProviderSystemName.ToLowerInvariant()
            select q).FirstOrDefault();

        if (associationRecord == null)
            return null;

        return await _customerService.GetCustomerById(associationRecord.CustomerId);
    }

    public virtual async Task<IList<ExternalAuthentication>> GetExternalIdentifiers(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var query = from p in _externalAuthenticationRecordRepository.Table
            where p.CustomerId == customer.Id
            select p;
        return await _externalAuthenticationRecordRepository.ToListAsync(query);
    }

    /// <summary>
    ///     Delete the external authentication
    /// </summary>
    /// <param name="externalAuthentication">External authentication</param>
    public virtual async Task DeleteExternalAuthentication(ExternalAuthentication externalAuthentication)
    {
        ArgumentNullException.ThrowIfNull(externalAuthentication);

        await _externalAuthenticationRecordRepository.DeleteAsync(externalAuthentication);
    }

    #endregion
}