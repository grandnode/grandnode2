using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Maps a validated access token or a browser sign-in to a customer: stored subject link; customer with the verified e-mail (link
///     stored and marked as made by e-mail; never a back-office account or an account with two-factor authentication, which connect
///     only with their password); registration with the Registered group only. A link made by e-mail is re-checked on every use
///     against the rules of a first link. Only active, registered, non-system accounts are used,
///     and one customer holds at most one identity (enforced by the link's identifier, see <see cref="OidcLinkId" />).
///     With per-store identity a subject has one link per store (plus the link of a customer of no store, valid in
///     every store). A unique index on (provider, identifier, store) makes the first concurrent requests of one user
///     converge on one account.
/// </summary>
public class OidcCustomerResolver : IOidcCustomerResolver
{
    public const string ProviderSystemName = "ExternalAuth.OpenIdConnect";

    public const string PasswordRequiredDescription =
        "The account must be connected to the identity with its password first";

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

        var link = await GetLink(sub, storeId);
        if (link != null)
        {
            var linkedCustomer = await _customerService.GetCustomerById(link.CustomerId);
            var linkedResult = await Usable(linkedCustomer, sub, storeId);
            //a link made by e-mail proves no more than the e-mail: it stops serving an account that has since
            //become one that connects only with its password
            if (linkedResult.Succeeded && link.LinkedByEmail && await RequiresPassword(linkedCustomer, storeId))
                return PasswordRequired(linkedCustomer, sub);
            return linkedResult;
        }

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

        //one identity per customer - a second subject with the same e-mail must not take over the account; the
        //link's identifier enforces it against concurrent requests, this check gives the answer in the normal case
        if (await HasOtherLink(customer, sub))
            return LinkedToOtherIdentity(customer, sub);

        //a back-office account and an account protected by two-factor authentication are never linked by e-mail,
        //verified or not; they connect with the account's password (browser sign-in), and tokens then use that link
        if (await RequiresPassword(customer, storeId))
            return PasswordRequired(customer, sub);

        var (linked, other) = await LinkOrGetWinner(customer, sub, email, storeId, true);
        if (other != null)
            return LinkedToOtherIdentity(customer, sub);
        if (linked?.Id != customer.Id)
            return await Usable(linked, sub, storeId);

        _logger.LogInformation("Access token subject {Subject} linked to existing customer {CustomerId} (store {StoreId})",
            sub, customer.Id, storeId);
        return result;
    }

    public virtual async Task<OidcLoginResult> ResolveForLogin(string sub, string email, bool emailVerified, string storeId)
    {
        if (string.IsNullOrEmpty(sub))
            return OidcLoginResult.Refused(OidcLoginRefusal.InvalidIdentity);

        var link = await GetLink(sub, storeId);
        if (link != null)
        {
            var linked = await _customerService.GetCustomerById(link.CustomerId);
            var reason = await Check(linked, sub, storeId);
            if (reason != Unusable.None)
                return OidcLoginResult.Refused(ToRefusal(reason));

            //a link made by e-mail (by an access token or an earlier sign-in) is held to the rules of a first link
            if (link.LinkedByEmail && (!emailVerified || await RequiresPassword(linked, storeId)))
                return OidcLoginResult.LinkRequired(linked);

            return OidcLoginResult.SignIn(linked);
        }

        if (string.IsNullOrWhiteSpace(email))
            return OidcLoginResult.Refused(OidcLoginRefusal.InvalidIdentity);

        var customer = await _customerService.GetCustomerByEmail(email,
            _customerConfig.RegisterCustomersPerStore ? storeId : "");
        if (customer == null)
        {
            var customerSettings = await _settingService.LoadSetting<CustomerSettings>(storeId);
            if (customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            {
                _logger.LogWarning("Sign-in subject {Subject}: registration is disabled in store {StoreId}", sub, storeId);
                return OidcLoginResult.Refused(OidcLoginRefusal.RegistrationDisabled);
            }

            return OidcLoginResult.RegistrationRequired();
        }

        var usable = await Check(customer, sub, storeId);
        if (usable != Unusable.None)
            return OidcLoginResult.Refused(ToRefusal(usable));

        if (await HasOtherLink(customer, sub))
        {
            _logger.LogWarning("Sign-in subject {Subject}: customer {CustomerId} is linked to another identity", sub, customer.Id);
            return OidcLoginResult.Refused(OidcLoginRefusal.LinkedToOtherIdentity);
        }

        //the first link of a back-office account, of an account protected by two-factor authentication, and of any account
        //whose e-mail nobody verified, needs the account's password (and its second factor)
        if (!emailVerified || await RequiresPassword(customer, storeId))
            return OidcLoginResult.LinkRequired(customer);

        return await Link(customer, sub, email, storeId, true) switch {
            OidcLinkResult.Linked => OidcLoginResult.SignIn(customer),
            _ => OidcLoginResult.Refused(OidcLoginRefusal.LinkedToOtherIdentity)
        };
    }

    public virtual Task<OidcLinkResult> LinkIdentity(Customer customer, string sub, string email, string storeId) =>
        Link(customer, sub, email, storeId, false);

    private async Task<OidcLinkResult> Link(Customer customer, string sub, string email, string storeId, bool byEmail)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentException.ThrowIfNullOrEmpty(sub);

        //the link this store would use - with per-store identity a link of another store does not block this one
        var existing = await GetLink(sub, storeId);
        if (existing != null)
        {
            if (existing.CustomerId != customer.Id)
                return OidcLinkResult.SubjectLinkedToOtherCustomer;

            //the account's password now stands behind a link made by e-mail
            if (!byEmail && existing.LinkedByEmail)
                await _externalAuthenticationRepository.UpdateField(existing.Id, x => x.LinkedByEmail, false);
            return OidcLinkResult.Linked;
        }

        if (await HasOtherLink(customer, sub))
            return OidcLinkResult.CustomerLinkedToOtherSubject;

        var (winner, other) = await LinkOrGetWinner(customer, sub, email ?? customer.Email ?? "", storeId, byEmail);
        if (other != null)
            return OidcLinkResult.CustomerLinkedToOtherSubject;
        if (winner?.Id != customer.Id)
            return OidcLinkResult.SubjectLinkedToOtherCustomer;

        _logger.LogInformation("Subject {Subject} linked to customer {CustomerId}", sub, customer.Id);
        return OidcLinkResult.Linked;
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
        var existing = await GetLink(sub, storeId);
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

        var (linked, other) = await LinkOrGetWinner(customer, sub, email, storeId, false);
        if (other != null || linked?.Id != customer.Id)
        {
            //lost the race: drop the account just created and use the one the winner linked
            await _customerService.DeleteCustomer(customer, true);
            return other != null ? LinkedToOtherIdentity(customer, sub) : await Usable(linked, sub, storeId);
        }

        //only after the link is stored, so the loser of a race sends no welcome message
        await _mediator.Publish(new CustomerRegisteredEvent(customer));
        _logger.LogInformation("Access token subject {Subject} registered as customer {CustomerId} (store {StoreId})",
            sub, customer.Id, storeId);
        return OidcCustomerResult.Success(customer);
    }

    /// <summary>
    ///     The link of the subject usable in the store. With per-store identity: the link of the store, otherwise
    ///     the link without a store (empty or, for older records, missing) unless its customer belongs to another
    ///     store - such a link was stored before customers were registered per store and must not keep the
    ///     subject from linking this store's customer; without per-store identity: any link.
    ///     Subjects are case-sensitive - exact comparison.
    /// </summary>
    private async Task<ExternalAuthentication> GetLink(string sub, string storeId)
    {
        if (!_customerConfig.RegisterCustomersPerStore)
            return await _externalAuthenticationRepository.GetOneAsync(x =>
                x.ProviderSystemName == ProviderSystemName && x.ExternalIdentifier == sub);

        if (!string.IsNullOrEmpty(storeId))
        {
            var storeLink = await _externalAuthenticationRepository.GetOneAsync(x =>
                x.ProviderSystemName == ProviderSystemName && x.ExternalIdentifier == sub && x.StoreId == storeId);
            if (storeLink != null)
                return storeLink;
        }

        var globalLink = await _externalAuthenticationRepository.GetOneAsync(x =>
            x.ProviderSystemName == ProviderSystemName && x.ExternalIdentifier == sub &&
            (x.StoreId == null || x.StoreId == ""));
        if (globalLink == null)
            return null;

        var linkedCustomer = await _customerService.GetCustomerById(globalLink.CustomerId);
        if (linkedCustomer != null && !string.IsNullOrEmpty(linkedCustomer.StoreId) && linkedCustomer.StoreId != storeId)
            return null;

        return globalLink;
    }

    /// <summary>
    ///     Store part of the link key: the customer's store with per-store identity, empty otherwise - never null,
    ///     so the unique index sees one value for "no store"
    /// </summary>
    private string LinkStoreId(Customer customer) =>
        _customerConfig.RegisterCustomersPerStore ? customer.StoreId ?? "" : "";

    private OidcCustomerResult LinkedToOtherIdentity(Customer customer, string sub)
    {
        _logger.LogWarning("Access token subject {Subject}: customer {CustomerId} is already linked to another subject",
            sub, customer.Id);
        return OidcCustomerResult.Forbidden("The customer account is linked to another identity");
    }

    private OidcCustomerResult PasswordRequired(Customer customer, string sub)
    {
        _logger.LogWarning("Access token subject {Subject}: customer {CustomerId} must be connected with its password",
            sub, customer.Id);
        return OidcCustomerResult.Forbidden(PasswordRequiredDescription);
    }

    /// <summary>
    ///     Stores the link under the customer's <see cref="OidcLinkId" />. When a concurrent request stored a link
    ///     first: the link of another subject now holding this customer (OtherLink), or the customer the subject's
    ///     winning link points to (Customer). Rethrows when the insert failed for another reason.
    /// </summary>
    private async Task<(Customer Customer, ExternalAuthentication OtherLink)> LinkOrGetWinner(Customer customer,
        string sub, string email, string storeId, bool byEmail)
    {
        var linkId = OidcLinkId(customer.Id);
        try
        {
            await _externalAuthenticationRepository.InsertAsync(new ExternalAuthentication {
                Id = linkId,
                CustomerId = customer.Id,
                Email = email.ToLowerInvariant(),
                ExternalIdentifier = sub,
                ExternalDisplayIdentifier = email,
                ProviderSystemName = ProviderSystemName,
                StoreId = LinkStoreId(customer),
                LinkedByEmail = byEmail
            });
            return (customer, null);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Storing the identity link for subject {Subject} failed ({ExceptionType}); re-reading",
                sub, ex.GetType().Name);
            var customerLink = await _externalAuthenticationRepository.GetByIdAsync(linkId);
            if (customerLink != null && customerLink.ExternalIdentifier != sub)
                return (null, customerLink);
            var winner = await GetLink(sub, storeId);
            if (winner == null)
                throw;
            return (await _customerService.GetCustomerById(winner.CustomerId), null);
        }
    }

    /// <summary>
    ///     Identifier of a customer's OIDC link: the first 12 bytes of SHA-256 over "ExternalAuth.OpenIdConnect:" +
    ///     customer id, as 24 lower-case hex characters (shaped like an ObjectId). Every OIDC link gets it, so the
    ///     primary key lets one customer hold one OIDC link - on MongoDB and LiteDB, without an extra index and
    ///     without restricting other providers. Lookups go by subject or customer (only a failed insert reads by
    ///     this value), so links stored with a random identifier keep working.
    /// </summary>
    public static string OidcLinkId(string customerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerId);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ProviderSystemName + ":" + customerId));
        return Convert.ToHexStringLower(hash, 0, 12);
    }

    /// <summary>
    ///     The checks every other login path applies: an active, registered, non-system account, and with per-store
    ///     identity one of the request's store (or of no store)
    /// </summary>
    private enum Unusable { None, Missing, Inactive, Unusable, OtherStore }

    //the checks every other login path applies: an active, registered, non-system account, and with per-store
    //identity one of the request's store (or of no store)
    private async Task<Unusable> Check(Customer customer, string sub, string storeId)
    {
        if (customer == null)
        {
            _logger.LogWarning("Subject {Subject} is linked to a customer that no longer exists", sub);
            return Unusable.Missing;
        }

        if (!customer.Active || customer.Deleted)
        {
            _logger.LogWarning("Subject {Subject}: customer {CustomerId} is inactive or deleted", sub, customer.Id);
            return customer.Deleted ? Unusable.Unusable : Unusable.Inactive;
        }

        if (customer.IsSystemAccount() || !await _groupService.IsRegistered(customer))
        {
            _logger.LogWarning("Subject {Subject}: customer {CustomerId} is a system or non-registered account", sub, customer.Id);
            return Unusable.Unusable;
        }

        if (_customerConfig.RegisterCustomersPerStore && !string.IsNullOrEmpty(customer.StoreId) && customer.StoreId != storeId)
        {
            _logger.LogWarning("Subject {Subject}: customer {CustomerId} belongs to store {CustomerStoreId}, not {StoreId}",
                sub, customer.Id, customer.StoreId, storeId);
            return Unusable.OtherStore;
        }

        return Unusable.None;
    }

    private async Task<OidcCustomerResult> Usable(Customer customer, string sub, string storeId) =>
        await Check(customer, sub, storeId) switch {
            Unusable.None => OidcCustomerResult.Success(customer),
            Unusable.Missing => OidcCustomerResult.Forbidden("The customer account no longer exists"),
            Unusable.Inactive => OidcCustomerResult.Forbidden("The customer account is inactive"),
            Unusable.OtherStore => OidcCustomerResult.Forbidden("The account belongs to another store"),
            _ => customer.Deleted
                ? OidcCustomerResult.Forbidden("The customer account is inactive")
                : OidcCustomerResult.Forbidden("The customer account cannot be used")
        };

    private static OidcLoginRefusal ToRefusal(Unusable reason) =>
        reason == Unusable.Inactive ? OidcLoginRefusal.AccountInactive : OidcLoginRefusal.AccountUnusable;

    private async Task<bool> HasOtherLink(Customer customer, string sub) =>
        await _externalAuthenticationRepository.GetOneAsync(x =>
            x.ProviderSystemName == ProviderSystemName && x.CustomerId == customer.Id && x.ExternalIdentifier != sub) != null;

    //the same condition the password sign-in uses to ask for the second factor
    private async Task<bool> HasTwoFactor(Customer customer, string storeId) =>
        customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled) &&
        (await _settingService.LoadSetting<CustomerSettings>(storeId)).TwoFactorAuthenticationEnabled;

    //accounts that are never linked by e-mail alone, and whose links made by e-mail stop serving them
    private async Task<bool> RequiresPassword(Customer customer, string storeId) =>
        await IsBackOfficeAccount(customer) || await HasTwoFactor(customer, storeId);

    private async Task<bool> IsBackOfficeAccount(Customer customer) =>
        !string.IsNullOrEmpty(customer.VendorId) || !string.IsNullOrEmpty(customer.StaffStoreId) ||
        await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer);
}
