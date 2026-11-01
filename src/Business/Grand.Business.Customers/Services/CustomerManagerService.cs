using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Customers.Services;

/// <summary>
///     Customer manager service
/// </summary>
public class CustomerManagerService : ICustomerManagerService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="customerService">Customer service</param>
    /// <param name="groupService">Group service</param>
    /// <param name="encryptionService">Encryption service</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="customerHistoryPasswordService">History password</param>
    /// <param name="customerSettings">Customer settings</param>
    /// <param name="externalSignInPolicy">Accounts that must sign in with an external provider</param>
    /// <param name="logger">Logger</param>
    public CustomerManagerService(
        ICustomerService customerService,
        IGroupService groupService,
        IEncryptionService encryptionService,
        IMediator mediator,
        ICustomerHistoryPasswordService customerHistoryPasswordService,
        CustomerSettings customerSettings,
        IExternalSignInPolicy externalSignInPolicy,
        ILogger<CustomerManagerService> logger)
    {
        _logger = logger;
        _externalSignInPolicy = externalSignInPolicy;
        _customerService = customerService;
        _groupService = groupService;
        _encryptionService = encryptionService;
        _mediator = mediator;
        _customerHistoryPasswordService = customerHistoryPasswordService;
        _customerSettings = customerSettings;
    }

    #endregion

    #region Fields

    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IEncryptionService _encryptionService;
    private readonly IMediator _mediator;
    private readonly ICustomerHistoryPasswordService _customerHistoryPasswordService;
    private readonly CustomerSettings _customerSettings;
    private readonly IExternalSignInPolicy _externalSignInPolicy;
    private readonly ILogger<CustomerManagerService> _logger;

    #endregion

    #region Methods

    public virtual bool PasswordMatch(PasswordFormat passwordFormat, string oldPassword, string newPassword,
        string passwordSalt)
    {
        //oldPassword is the stored credential, newPassword is the plain-text candidate being checked against it
        return _encryptionService.VerifyPassword(newPassword, passwordFormat, oldPassword, passwordSalt,
            _customerSettings.HashedPasswordFormat);
    }


    /// <summary>
    ///     Validate customer
    /// </summary>
    /// <param name="usernameOrEmail">Username or email</param>
    /// <param name="password">Password</param>
    /// <param name="storeId">Store identifier - used to resolve the customer when per-store customer identity is enabled</param>
    /// <param name="surface">Where the password is used: the sign-in policy decides per surface</param>
    /// <returns>Result</returns>
    public virtual async Task<CustomerLoginResults> LoginCustomer(string usernameOrEmail, string password,
        string storeId = "", SignInSurface surface = SignInSurface.Storefront)
    {
        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsername(usernameOrEmail, storeId)
            : await _customerService.GetCustomerByEmail(usernameOrEmail, storeId);

        var isValid = _encryptionService.VerifyPassword(password, customer.PasswordFormatId, customer.Password,
            customer.PasswordSalt, _customerSettings.HashedPasswordFormat);
        if (!isValid)
            return CustomerLoginResults.WrongPassword;

        //checked after the password, so the answer tells nothing to someone who does not know it
        var decision = await _externalSignInPolicy.PasswordSignIn(customer, surface);
        if (!decision.Allowed)
            return CustomerLoginResults.ExternalSignInRequired;
        if (decision.BreakGlass)
            _logger.LogWarning("Break-glass password sign-in of customer {CustomerId} on the Admin panel", customer.Id);

        //transparently migrate weak/legacy hashes (SHA-x, Clear, Encrypted) to the modern PBKDF2 format
        if (_encryptionService.PasswordHashNeedsUpgrade(customer.PasswordFormatId, customer.Password))
        {
            try
            {
                await UpgradePasswordHash(customer, password);
            }
            catch
            {
                //best-effort upgrade only; don't block login if persisting the upgraded hash fails
            }
        }
        //2fa required
        if (customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled) &&
            _customerSettings.TwoFactorAuthenticationEnabled)
            return CustomerLoginResults.RequiresTwoFactor;

        return CustomerLoginResults.Successful;
    }

    /// <summary>
    ///     Register customer
    /// </summary>
    /// <param name="request">Request</param>
    /// <returns>Result</returns>
    public virtual async Task RegisterCustomer(RegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Customer == null)
            throw new ArgumentException("Can't load current customer");

        //event notification
        await _mediator.CustomerRegistrationEvent(request);

        request.Customer.Username = request.Username;
        request.Customer.Email = request.Email;
        request.Customer.PasswordFormatId = request.PasswordFormat;
        request.Customer.StoreId = request.StoreId;

        switch (request.PasswordFormat)
        {
            case PasswordFormat.Clear:
                request.Customer.Password = request.Password;
                break;
            case PasswordFormat.Encrypted:
                request.Customer.PasswordSalt = CommonHelper.GenerateRandomDigitCode(24);
                request.Customer.Password =
                    _encryptionService.EncryptText(request.Password, request.Customer.PasswordSalt);
                break;
            case PasswordFormat.Hashed:
                //modern self-describing PBKDF2 hash - salt and parameters are embedded in the value itself
                request.Customer.PasswordSalt = string.Empty;
                request.Customer.Password = _encryptionService.HashPassword(request.Password);
                break;
        }

        await _customerHistoryPasswordService.InsertCustomerPassword(request.Customer);

        request.Customer.Active = request.IsApproved;
        await _customerService.UpdateActive(request.Customer);
        //add to 'Registered' role
        var registeredRole = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
        if (registeredRole == null)
            throw new GrandException("'Registered' role could not be loaded");
        request.Customer.Groups.Add(registeredRole.Id);
        await _customerService.InsertCustomerGroupInCustomer(registeredRole, request.Customer.Id);
        //remove from 'Guests' role
        var guestGroup = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Guests);
        var guestExists = request.Customer.Groups.FirstOrDefault(cr => cr == guestGroup?.Id);
        if (guestExists != null)
        {
            request.Customer.Groups.Remove(guestGroup.Id);
            await _customerService.DeleteCustomerGroupInCustomer(guestGroup, request.Customer.Id);
        }

        request.Customer.PasswordChangeDateUtc = DateTime.UtcNow;
        await _customerService.UpdateCustomer(request.Customer);
    }

    /// <summary>
    ///     Change password
    /// </summary>
    /// <param name="request">Request</param>
    public virtual async Task ChangePassword(ChangePasswordRequest request, string storeId = "")
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await _customerService.GetCustomerByEmail(request.Email, storeId);

        await ChangePassword(customer, request.PasswordFormat, request.NewPassword);
    }

    /// <summary>
    ///     Change password of this customer, without resolving the account by e-mail
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="passwordFormat">Password format</param>
    /// <param name="newPassword">New password</param>
    public virtual async Task ChangePassword(Customer customer, PasswordFormat passwordFormat, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(customer);

        switch (passwordFormat)
        {
            case PasswordFormat.Clear:
            {
                customer.Password = newPassword;
            }
                break;
            case PasswordFormat.Encrypted:
            {
                customer.PasswordSalt = CommonHelper.GenerateRandomDigitCode(24);
                customer.Password = _encryptionService.EncryptText(newPassword, customer.PasswordSalt);
            }
                break;
            case PasswordFormat.Hashed:
            {
                //modern self-describing PBKDF2 hash - salt and parameters are embedded in the value itself
                customer.PasswordSalt = string.Empty;
                customer.Password = _encryptionService.HashPassword(newPassword);
            }
                break;
        }

        customer.PasswordChangeDateUtc = DateTime.UtcNow;
        customer.PasswordFormatId = passwordFormat;
        //only the password fields: the caller's copy may be older than the account
        await _customerService.UpdateCustomerPassword(customer);
        //insert password history
        await _customerHistoryPasswordService.InsertCustomerPassword(customer);

        //create new login token
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.PasswordToken, Guid.NewGuid().ToString());
    }

    /// <summary>
    ///     Re-hashes an already-verified plain-text password to the modern PBKDF2 format and persists it.
    ///     Called right after a successful authentication against a weak/legacy credential, so no password reset is
    ///     required and no schema change is involved (the value simply moves to the self-describing hash format).
    /// </summary>
    private async Task UpgradePasswordHash(Customer customer, string plainPassword)
    {
        customer.Password = _encryptionService.HashPassword(plainPassword);
        customer.PasswordSalt = string.Empty;
        customer.PasswordFormatId = PasswordFormat.Hashed;
        await _customerService.UpdateCustomer(customer);
    }

    #endregion
}