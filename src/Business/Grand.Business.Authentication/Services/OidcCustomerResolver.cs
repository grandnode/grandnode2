using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Maps a validated access token to a customer: stored subject link; customer with the verified e-mail (link
///     stored); registration with the Registered group only. Only active, registered, non-system accounts are used,
///     and one customer holds at most one identity. A unique index on (provider, identifier) makes the
///     first concurrent requests of one user converge on one account.
/// </summary>
public class OidcCustomerResolver : IOidcCustomerResolver
{
    public const string ProviderSystemName = "ExternalAuth.OpenIdConnect";

    private readonly IRepository<ExternalAuthentication> _externalAuthenticationRepository;
    private readonly ICustomerService _customerService;
    private readonly ICustomerManagerService _customerManagerService;
    private readonly ISettingService _settingService;
    private readonly IGroupService _groupService;
    private readonly IPermissionService _permissionService;
    private readonly IMediator _mediator;
    private readonly OidcResourceServerConfig _config;
    private readonly CustomerConfig _customerConfig;
    private readonly ILogger<OidcCustomerResolver> _logger;

    public OidcCustomerResolver(
        IRepository<ExternalAuthentication> externalAuthenticationRepository,
        ICustomerService customerService,
        ICustomerManagerService customerManagerService,
        ISettingService settingService,
        IGroupService groupService,
        IPermissionService permissionService,
        IMediator mediator,
        OidcResourceServerConfig config,
        CustomerConfig customerConfig,
        ILogger<OidcCustomerResolver> logger)
    {
        _externalAuthenticationRepository = externalAuthenticationRepository;
        _customerService = customerService;
        _customerManagerService = customerManagerService;
        _settingService = settingService;
        _groupService = groupService;
        _permissionService = permissionService;
        _mediator = mediator;
        _config = config;
        _customerConfig = customerConfig;
        _logger = logger;
    }

    public virtual async Task<OidcCustomerResult> Resolve(ClaimsPrincipal principal, string storeId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var sub = principal.FindFirst(_config.Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(sub))
            return OidcCustomerResult.InvalidToken("The token has no subject");

        var link = await GetLink(sub);
        if (link != null)
            return await Usable(await _customerService.GetCustomerById(link.CustomerId), sub, storeId);

        var email = principal.FindFirst(_config.Claims.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return OidcCustomerResult.InvalidToken("The token has no e-mail address");

        if (_config.RequireEmailVerified &&
            !string.Equals(principal.FindFirst(_config.Claims.EmailVerified)?.Value, "true", StringComparison.OrdinalIgnoreCase))
            return OidcCustomerResult.InvalidToken("The e-mail address in the token is not verified");

        //without per-store identity the e-mail is unique across stores - a store-scoped lookup would miss customers
        //of other stores and register a duplicate
        var customer = await _customerService.GetCustomerByEmail(email,
            _customerConfig.RegisterCustomersPerStore ? storeId : "");
        if (customer == null)
            return await Register(sub, email, storeId);

        var result = await Usable(customer, sub, storeId);
        if (!result.Succeeded)
            return result;

        //an e-mail nobody verified could take over a back-office account, so those are never linked by it
        if (!_config.RequireEmailVerified && await IsBackOfficeAccount(customer))
        {
            _logger.LogWarning("Access token subject {Subject}: customer {CustomerId} is a back-office account and the e-mail is not verified",
                sub, customer.Id);
            return OidcCustomerResult.Forbidden("Back-office accounts can only be linked with a verified e-mail");
        }

        //one identity per customer - a second subject with the same e-mail must not take over the account
        var otherLink = await _externalAuthenticationRepository.GetOneAsync(x =>
            x.ProviderSystemName == ProviderSystemName && x.CustomerId == customer.Id && x.ExternalIdentifier != sub);
        if (otherLink != null)
        {
            _logger.LogWarning("Access token subject {Subject}: customer {CustomerId} is already linked to subject {LinkedSubject}",
                sub, customer.Id, otherLink.ExternalIdentifier);
            return OidcCustomerResult.Forbidden("The customer account is linked to another identity");
        }

        var linked = await LinkOrGetWinner(customer, sub, email);
        if (linked?.Id != customer.Id)
            return await Usable(linked, sub, storeId);

        _logger.LogInformation("Access token subject {Subject} linked to existing customer {CustomerId} (store {StoreId})",
            sub, customer.Id, storeId);
        return result;
    }

    private async Task<OidcCustomerResult> Register(string sub, string email, string storeId)
    {
        if (!_config.AutoRegister)
        {
            _logger.LogWarning("Access token subject {Subject}: no customer matches and automatic registration is off", sub);
            return OidcCustomerResult.Forbidden("No customer account matches the token");
        }

        var customerSettings = await _settingService.LoadSetting<CustomerSettings>(storeId);
        if (customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
        {
            _logger.LogWarning("Access token subject {Subject}: registration is disabled in store {StoreId}", sub, storeId);
            return OidcCustomerResult.Forbidden("Registration is disabled");
        }

        //a concurrent request may have registered this subject since the first lookup
        var existing = await GetLink(sub);
        if (existing != null)
            return await Usable(await _customerService.GetCustomerById(existing.CustomerId), sub, storeId);

        //the e-mail becomes the user name - another account may hold it as its user name without that e-mail
        var normalizedEmail = email.ToLowerInvariant();
        if (await _customerService.GetCustomerByUsername(normalizedEmail,
                _customerConfig.RegisterCustomersPerStore ? storeId : "") != null)
        {
            _logger.LogWarning("Access token subject {Subject}: the user name is already taken in store {StoreId}", sub, storeId);
            return OidcCustomerResult.Forbidden("The user name is already taken");
        }

        var customer = await _customerService.InsertGuestCustomer(new Customer {
            CustomerGuid = Guid.NewGuid(),
            Active = true,
            StoreId = storeId,
            LastActivityDateUtc = DateTime.UtcNow
        });

        //access comes from permissions an admin grants (and the provider's e-mail check is configurable), so the store's e-mail
        //validation / admin approval does not apply; the account gets the Registered group only
        await _customerManagerService.RegisterCustomer(new RegistrationRequest(customer, normalizedEmail,
            normalizedEmail, CommonHelper.GenerateRandomDigitCode(20), PasswordFormat.Hashed, storeId));

        var linked = await LinkOrGetWinner(customer, sub, email);
        if (linked?.Id != customer.Id)
        {
            //lost the race: drop the account just created and use the one the winner linked
            await _customerService.DeleteCustomer(customer, true);
            return await Usable(linked, sub, storeId);
        }

        //only after the link is stored, so the loser of a race sends no welcome message
        await _mediator.Publish(new CustomerRegisteredEvent(customer));
        _logger.LogInformation("Access token subject {Subject} registered as customer {CustomerId} (store {StoreId})",
            sub, customer.Id, storeId);
        return OidcCustomerResult.Success(customer);
    }

    //subjects are case-sensitive - exact comparison
    private Task<ExternalAuthentication> GetLink(string sub) =>
        _externalAuthenticationRepository.GetOneAsync(x =>
            x.ProviderSystemName == ProviderSystemName && x.ExternalIdentifier == sub);

    /// <summary>
    ///     Stores the link; when a concurrent request stored one first, returns the customer of that link. Rethrows
    ///     when the insert failed for another reason.
    /// </summary>
    private async Task<Customer> LinkOrGetWinner(Customer customer, string sub, string email)
    {
        try
        {
            await _externalAuthenticationRepository.InsertAsync(new ExternalAuthentication {
                CustomerId = customer.Id,
                Email = email.ToLowerInvariant(),
                ExternalIdentifier = sub,
                ExternalDisplayIdentifier = email,
                ProviderSystemName = ProviderSystemName
            });
            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Storing the identity link for subject {Subject} failed ({ExceptionType}); re-reading",
                sub, ex.GetType().Name);
            var winner = await GetLink(sub);
            if (winner == null)
                throw;
            return await _customerService.GetCustomerById(winner.CustomerId);
        }
    }

    /// <summary>
    ///     The checks every other login path applies: an active, registered, non-system account, and with per-store
    ///     identity one of the request's store (or of no store)
    /// </summary>
    private async Task<OidcCustomerResult> Usable(Customer customer, string sub, string storeId)
    {
        if (customer == null)
        {
            _logger.LogWarning("Access token subject {Subject} is linked to a customer that no longer exists", sub);
            return OidcCustomerResult.Forbidden("The customer account no longer exists");
        }

        if (!customer.Active || customer.Deleted)
        {
            _logger.LogWarning("Access token subject {Subject}: customer {CustomerId} is inactive or deleted", sub, customer.Id);
            return OidcCustomerResult.Forbidden("The customer account is inactive");
        }

        if (customer.IsSystemAccount() || !await _groupService.IsRegistered(customer))
        {
            _logger.LogWarning("Access token subject {Subject}: customer {CustomerId} is a system or non-registered account",
                sub, customer.Id);
            return OidcCustomerResult.Forbidden("The customer account cannot be used");
        }

        if (_customerConfig.RegisterCustomersPerStore && !string.IsNullOrEmpty(customer.StoreId) &&
            customer.StoreId != storeId)
        {
            _logger.LogWarning("Access token subject {Subject}: customer {CustomerId} belongs to store {CustomerStoreId}, not {StoreId}",
                sub, customer.Id, customer.StoreId, storeId);
            return OidcCustomerResult.Forbidden("The account belongs to another store");
        }

        return OidcCustomerResult.Success(customer);
    }

    private async Task<bool> IsBackOfficeAccount(Customer customer) =>
        !string.IsNullOrEmpty(customer.VendorId) || !string.IsNullOrEmpty(customer.StaffStoreId) ||
        await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer);
}
