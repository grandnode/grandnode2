using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Represents service using cookie middleware for the authentication
/// </summary>
public class CookieAuthenticationService : IGrandAuthenticationService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="customerSettings">Customer settings</param>
    /// <param name="customerService">Customer service</param>
    /// <param name="groupService">Group service</param>
    /// <param name="httpContextAccessor">HTTP context accessor</param>
    /// <param name="securityConfig">SecurityConfig</param>
    public CookieAuthenticationService(
        CustomerSettings customerSettings,
        ICustomerService customerService,
        IGroupService groupService,
        IHttpContextAccessor httpContextAccessor,
        SecurityConfig securityConfig)
    {
        _customerSettings = customerSettings;
        _customerService = customerService;
        _groupService = groupService;
        _httpContextAccessor = httpContextAccessor;
        _securityConfig = securityConfig;
    }

    #endregion

    #region Const

    private string CustomerCookieName => $"{_securityConfig.CookiePrefix}Customer";

    #endregion

    #region Fields

    private readonly CustomerSettings _customerSettings;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SecurityConfig _securityConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Sign in
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="isPersistent">Whether the authentication session is persisted across multiple requests</param>
    public virtual async Task SignIn(Customer customer, bool isPersistent)
    {
        ArgumentNullException.ThrowIfNull(customer);

        //create claims for customer's username and email
        var claims = new List<Claim>();

        if (!string.IsNullOrEmpty(customer.Username))
            claims.Add(new Claim(ClaimTypes.Name, customer.Username, ClaimValueTypes.String,
                _securityConfig.CookieClaimsIssuer));

        if (!string.IsNullOrEmpty(customer.Email))
            claims.Add(new Claim(ClaimTypes.Email, customer.Email, ClaimValueTypes.Email,
                _securityConfig.CookieClaimsIssuer));

        //add token
        var passwordToken = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordToken);
        if (string.IsNullOrEmpty(passwordToken))
        {
            var passwordGuid = Guid.NewGuid().ToString();
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.PasswordToken, passwordGuid);
            claims.Add(new Claim(ClaimTypes.UserData, passwordGuid, ClaimValueTypes.String,
                _securityConfig.CookieClaimsIssuer));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.UserData, passwordToken, ClaimValueTypes.String,
                _securityConfig.CookieClaimsIssuer));
        }

        //create principal for the present scheme of authentication
        var userIdentity = new ClaimsIdentity(claims, GrandCookieAuthenticationDefaults.AuthenticationScheme);
        var userPrincipal = new ClaimsPrincipal(userIdentity);

        //set value that indicates whether the session is persisted and the time at which the authentication was issued
        var authenticationProperties = new AuthenticationProperties {
            IsPersistent = isPersistent,
            IssuedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddHours(_securityConfig.CookieAuthExpires)
        };

        //sign in user
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(CustomerCookieName);

            await _httpContextAccessor.HttpContext.SignInAsync(GrandCookieAuthenticationDefaults.AuthenticationScheme, userPrincipal, authenticationProperties);
        }
    }

    /// <summary>
    ///     Sign out customer
    /// </summary>
    public virtual async Task SignOut()
    {
        //and then sign out customer from the present scheme of authentication
        if (_httpContextAccessor.HttpContext != null)
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(GrandCookieAuthenticationDefaults
                .AuthenticationScheme);
            //sign out also from other schema
            await _httpContextAccessor.HttpContext.SignOutAsync(GrandCookieAuthenticationDefaults
                .ExternalAuthenticationScheme);
        }
    }

    /// <summary>
    ///     Get an authenticated customer
    /// </summary>
    /// <returns>Customer</returns>
    public virtual async Task<Customer> GetAuthenticatedCustomer()
    {
        var authenticateResult = await _httpContextAccessor.HttpContext.AuthenticateAsync(GrandCookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return null;

        var customer = await RetrieveCustomer(authenticateResult.Principal);
        if (customer == null || !await IsValidCustomer(customer, authenticateResult.Principal))
            return null;

        return customer;
    }

    private async Task<Customer> RetrieveCustomer(ClaimsPrincipal principal)
    {
        if (_customerSettings.UsernamesEnabled)
        {
            var username = principal.FindFirst(claim =>
                claim.Type == ClaimTypes.Name &&
                claim.Issuer.Equals(_securityConfig.CookieClaimsIssuer, StringComparison.InvariantCultureIgnoreCase))
                ?.Value;

            if (!string.IsNullOrEmpty(username))
                return await _customerService.GetCustomerByUsername(username);
        }
        else
        {
            var email = principal.FindFirst(claim =>
                claim.Type == ClaimTypes.Email &&
                claim.Issuer.Equals(_securityConfig.CookieClaimsIssuer, StringComparison.InvariantCultureIgnoreCase))
                ?.Value;

            if (!string.IsNullOrEmpty(email))
                return await _customerService.GetCustomerByEmail(email);
        }

        return null;
    }

    private async Task<bool> IsValidCustomer(Customer customer, ClaimsPrincipal principal)
    {
        var passwordToken = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordToken);
        if (!string.IsNullOrEmpty(passwordToken))
        {
            var tokenClaim = principal
                .FindFirst(claim =>
                    claim.Type == ClaimTypes.UserData &&
                    claim.Issuer.Equals(_securityConfig.CookieClaimsIssuer, StringComparison.InvariantCultureIgnoreCase));

            if (tokenClaim == null || tokenClaim.Value != passwordToken)
                return false;
        }

        if (!customer.Active || customer.Deleted || !await _groupService.IsRegistered(customer))
            return false;

        return true;
    }
    /// <summary>
    ///     Get customer cookie
    /// </summary>
    /// <returns>String value of cookie</returns>
    public virtual Task<string> GetCustomerGuid()
    {
        return _httpContextAccessor.HttpContext?.Request == null
            ? Task.FromResult<string>(null)
            : Task.FromResult(_httpContextAccessor.HttpContext.Request.Cookies[CustomerCookieName]);
    }

    /// <summary>
    ///     Set customer cookie
    /// </summary>
    /// <param name="customerGuid">Guid of the customer</param>
    public virtual Task SetCustomerGuid(Guid customerGuid)
    {
        if (_httpContextAccessor.HttpContext?.Response == null)
            return Task.CompletedTask;

        //Delete existing cookie value
        _httpContextAccessor.HttpContext.Response.Cookies.Delete(CustomerCookieName);

        //Get the date date of current cookie expiration
        var cookieExpiresDate = DateTime.UtcNow.AddHours(_securityConfig.CookieAuthExpires);

        //If provided guid is empty (only remove cookies)
        if (customerGuid == Guid.Empty)
            return Task.CompletedTask;

        //set new cookie value
        var options = new CookieOptions {
            HttpOnly = true,
            Expires = cookieExpiresDate
        };
        _httpContextAccessor.HttpContext.Response.Cookies.Append(CustomerCookieName, customerGuid.ToString(), options);

        return Task.CompletedTask;
    }

    #endregion
}