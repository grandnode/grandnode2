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
using System.Security.Cryptography;
using System.Text;

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
            //one account per provider: a second one of the same provider is refused
            var existingLink = await _externalAuthenticationRecordRepository.FirstOrDefaultAsync(
                LinksOfProvider(parameters.ProviderSystemName).Where(q => q.CustomerId == currentLoggedInUser.Id));
            if (existingLink != null)
            {
                //the provider issued a new identifier for the customer's own account (a replaced app, rotated
                //subject identifiers): refresh the stale link instead of locking the customer out
                if (!IsOwnEmail(currentLoggedInUser, parameters.Email))
                    return ExternalAuthResult.ProviderAlreadyConnected();

                await RefreshLink(existingLink, parameters);
                return ExternalAuthResult.Associated(currentLoggedInUser);
            }

            try
            {
                await AssociateCustomer(currentLoggedInUser, parameters);
            }
            catch (Exception)
            {
                //a concurrent request linked an account of this provider first: the link identifier is unique
                //per customer and provider, so the second insert fails. Any other failure is rethrown
                if (!await _externalAuthenticationRecordRepository.AnyAsync(
                        LinksOfProvider(parameters.ProviderSystemName)
                            .Where(q => q.CustomerId == currentLoggedInUser.Id)))
                    throw;
                return ExternalAuthResult.ProviderAlreadyConnected();
            }

            return ExternalAuthResult.Associated(currentLoggedInUser);
        }

        //the same rule as the registration form; the provider's e-mail never takes over an existing account. Checked
        //before the registration type: the owner signs in and connects the account, registration or not
        if (await IsEmailOrUsernameTaken(parameters.Email, storeId))
            return ExternalAuthResult.EmailAlreadyExists();

        //registration is disabled
        if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            return ExternalAuthResult.RegistrationDisabled();

        //the new account signs in and receives its mail with the provider's e-mail
        if (string.IsNullOrWhiteSpace(parameters.Email))
            return ExternalAuthResult.EmailRequired();

        return await RegisterNewUser(parameters, customer, storeId, language);
    }

    /// <summary>
    ///     The links of a provider, matched case-insensitively. A record without a provider name (old or imported
    ///     data) matches no provider instead of throwing when LiteDB evaluates the query in memory
    /// </summary>
    private IQueryable<ExternalAuthentication> LinksOfProvider(string providerSystemName)
    {
        var name = providerSystemName.ToLowerInvariant();
        return _externalAuthenticationRecordRepository.Table.Where(q =>
            q.ProviderSystemName != null && q.ProviderSystemName.ToLowerInvariant() == name);
    }

    /// <summary>
    ///     Identifier of a new link: the first 12 bytes of SHA-256 over "ExternalAuth:" + provider (lower case) + ":" +
    ///     customer id, as 24 lower-case hex characters (shaped like an ObjectId). The primary key then lets one
    ///     customer hold one link per provider, also when two requests link at the same time, on MongoDB and LiteDB
    ///     without an extra index. Lookups go by identifier or customer, so links stored with a random identifier
    ///     keep working.
    /// </summary>
    public static string LinkId(string providerSystemName, string customerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerSystemName);
        ArgumentException.ThrowIfNullOrEmpty(customerId);
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes("ExternalAuth:" + providerSystemName.ToLowerInvariant() + ":" + customerId));
        return Convert.ToHexStringLower(hash, 0, 12);
    }

    /// <summary>
    ///     Whether the provider's e-mail is the customer's own e-mail
    /// </summary>
    private static bool IsOwnEmail(Customer customer, string email)
    {
        return !string.IsNullOrEmpty(email) && string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Points an existing link of the provider at the identifier the provider now issues
    /// </summary>
    private async Task RefreshLink(ExternalAuthentication link, ExternalAuthParam parameters)
    {
        link.ExternalIdentifier = parameters.Identifier;
        link.ExternalDisplayIdentifier = parameters.Name;
        link.Email = parameters.Email;
        link.OAuthAccessToken = parameters.AccessToken;
        await _externalAuthenticationRecordRepository.UpdateAsync(link);
    }

    /// <summary>
    ///     Whether another customer has the provider's e-mail as their e-mail or, with usernames enabled, as their
    ///     username; registration stores the provider's e-mail in both fields
    /// </summary>
    private async Task<bool> IsEmailOrUsernameTaken(string email, string storeId)
    {
        return await _customerService.GetCustomerByEmail(email, storeId) != null ||
               (_customerSettings.UsernamesEnabled &&
                await _customerService.GetCustomerByUsername(email, storeId) != null);
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
            Id = LinkId(parameters.ProviderSystemName, customer.Id),
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

        var associationRecord = await _externalAuthenticationRecordRepository.FirstOrDefaultAsync(
            LinksOfProvider(parameters.ProviderSystemName).Where(q => q.ExternalIdentifier == parameters.Identifier));

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