using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using MediatR;

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
    /// <param name="userFieldService">UserFields service</param>
    /// <param name="customerHistoryPasswordService">History password</param>
    /// <param name="customerSettings">Customer settings</param>
    public CustomerManagerService(
        ICustomerService customerService,
        IGroupService groupService,
        IEncryptionService encryptionService,
        IMediator mediator,
        IUserFieldService userFieldService,
        ICustomerHistoryPasswordService customerHistoryPasswordService,
        CustomerSettings customerSettings)
    {
        _customerService = customerService;
        _groupService = groupService;
        _encryptionService = encryptionService;
        _mediator = mediator;
        _userFieldService = userFieldService;
        _customerHistoryPasswordService = customerHistoryPasswordService;
        _customerSettings = customerSettings;
    }

    #endregion

    #region Fields

    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IEncryptionService _encryptionService;
    private readonly IMediator _mediator;
    private readonly IUserFieldService _userFieldService;
    private readonly ICustomerHistoryPasswordService _customerHistoryPasswordService;
    private readonly CustomerSettings _customerSettings;

    #endregion

    #region Methods

    public virtual bool PasswordMatch(PasswordFormat passwordFormat, string oldPassword, string newPassword,
        string passwordSalt)
    {
        var newPwd = passwordFormat switch {
            PasswordFormat.Clear => newPassword,
            PasswordFormat.Encrypted => _encryptionService.EncryptText(newPassword, passwordSalt),
            PasswordFormat.Hashed => _encryptionService.CreatePasswordHash(newPassword, passwordSalt,
                _customerSettings.HashedPasswordFormat),
            _ => throw new Exception("PasswordFormat not supported")
        };
        return oldPassword.Equals(newPwd);
    }


    /// <summary>
    ///     Validate customer
    /// </summary>
    /// <param name="usernameOrEmail">Username or email</param>
    /// <param name="password">Password</param>
    /// <returns>Result</returns>
    public virtual async Task<CustomerLoginResults> LoginCustomer(string usernameOrEmail, string password)
    {
        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsername(usernameOrEmail)
            : await _customerService.GetCustomerByEmail(usernameOrEmail);

        var pwd = customer.PasswordFormatId switch {
            PasswordFormat.Clear => password,
            PasswordFormat.Encrypted => _encryptionService.EncryptText(password, customer.PasswordSalt),
            PasswordFormat.Hashed => _encryptionService.CreatePasswordHash(password, customer.PasswordSalt,
                _customerSettings.HashedPasswordFormat),
            _ => throw new Exception("PasswordFormat not supported")
        };
        var isValid = pwd == customer.Password;
        if (!isValid)
            return CustomerLoginResults.WrongPassword;

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
                var saltKey = _encryptionService.CreateSaltKey(5);
                request.Customer.PasswordSalt = saltKey;
                request.Customer.Password = _encryptionService.CreatePasswordHash(request.Password, saltKey,
                    _customerSettings.HashedPasswordFormat);
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
    public virtual async Task ChangePassword(ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await _customerService.GetCustomerByEmail(request.Email);
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        switch (request.PasswordFormat)
        {
            case PasswordFormat.Clear:
            {
                customer.Password = request.NewPassword;
            }
                break;
            case PasswordFormat.Encrypted:
            {
                customer.PasswordSalt = CommonHelper.GenerateRandomDigitCode(24);
                customer.Password = _encryptionService.EncryptText(request.NewPassword, customer.PasswordSalt);
            }
                break;
            case PasswordFormat.Hashed:
            {
                var saltKey = _encryptionService.CreateSaltKey(5);
                customer.PasswordSalt = saltKey;
                customer.Password = _encryptionService.CreatePasswordHash(request.NewPassword, saltKey,
                    _customerSettings.HashedPasswordFormat);
            }
                break;
        }

        customer.PasswordChangeDateUtc = DateTime.UtcNow;
        customer.PasswordFormatId = request.PasswordFormat;
        await _customerService.UpdateCustomer(customer);
        //insert password history
        await _customerHistoryPasswordService.InsertCustomerPassword(customer);

        //create new login token
        await _userFieldService.SaveField(customer, SystemCustomerFieldNames.PasswordToken, Guid.NewGuid().ToString());
    }

    #endregion
}