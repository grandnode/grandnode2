using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Maps a validated access token to a customer: stored subject link; customer with the verified e-mail (link
///     stored); registration with the Registered group only. A unique index on (provider, identifier) makes the
///     first concurrent requests of one user converge on one account.
/// </summary>
public class OidcCustomerResolver : IOidcCustomerResolver
{
    public const string ProviderSystemName = "Oidc";

    private readonly IRepository<ExternalAuthentication> _externalAuthenticationRepository;
    private readonly ICustomerService _customerService;
    private readonly ICustomerManagerService _customerManagerService;
    private readonly ISettingService _settingService;
    private readonly IMediator _mediator;
    private readonly OidcResourceServerConfig _config;
    private readonly CustomerConfig _customerConfig;
    private readonly ILogger<OidcCustomerResolver> _logger;

    public OidcCustomerResolver(
        IRepository<ExternalAuthentication> externalAuthenticationRepository,
        ICustomerService customerService,
        ICustomerManagerService customerManagerService,
        ISettingService settingService,
        IMediator mediator,
        OidcResourceServerConfig config,
        CustomerConfig customerConfig,
        ILogger<OidcCustomerResolver> logger)
    {
        _externalAuthenticationRepository = externalAuthenticationRepository;
        _customerService = customerService;
        _customerManagerService = customerManagerService;
        _settingService = settingService;
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
            return Usable(await _customerService.GetCustomerById(link.CustomerId), sub);

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

        var result = Usable(customer, sub);
        if (!result.Succeeded)
            return result;

        var linked = await LinkOrGetWinner(customer, sub, email);
        if (linked?.Id != customer.Id)
            return Usable(linked, sub);

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
            return Usable(await _customerService.GetCustomerById(existing.CustomerId), sub);

        var normalizedEmail = email.ToLowerInvariant();
        var customer = await _customerService.InsertGuestCustomer(new Customer {
            CustomerGuid = Guid.NewGuid(),
            Active = true,
            StoreId = storeId,
            LastActivityDateUtc = DateTime.UtcNow
        });

        //the provider verified the e-mail and access comes from permissions an admin grants, so the store's e-mail
        //validation / admin approval does not apply; the account gets the Registered group only
        await _customerManagerService.RegisterCustomer(new RegistrationRequest(customer, normalizedEmail,
            normalizedEmail, CommonHelper.GenerateRandomDigitCode(20), PasswordFormat.Hashed, storeId));

        var linked = await LinkOrGetWinner(customer, sub, email);
        if (linked?.Id != customer.Id)
        {
            //lost the race: drop the account just created and use the one the winner linked
            await _customerService.DeleteCustomer(customer, true);
            return Usable(linked, sub);
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
        catch (Exception)
        {
            var winner = await GetLink(sub);
            if (winner == null)
                throw;
            return await _customerService.GetCustomerById(winner.CustomerId);
        }
    }

    private OidcCustomerResult Usable(Customer customer, string sub)
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

        return OidcCustomerResult.Success(customer);
    }
}
