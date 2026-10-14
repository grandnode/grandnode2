using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Grand.Business.Authentication.Services;

public class JwtBearerCustomerAuthenticationService : IJwtBearerCustomerAuthenticationService
{
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IPermissionService _permissionService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IExternalSignInPolicy _externalSignInPolicy;
    private readonly OidcLoginConfig _oidcLoginConfig;
    private string _errorMessage;

    public JwtBearerCustomerAuthenticationService(ICustomerService customerService,
        IPermissionService permissionService, IGroupService groupService, IRefreshTokenService refreshTokenService,
        IExternalSignInPolicy externalSignInPolicy, OidcLoginConfig oidcLoginConfig)
    {
        _customerService = customerService;
        _permissionService = permissionService;
        _groupService = groupService;
        _refreshTokenService = refreshTokenService;
        _externalSignInPolicy = externalSignInPolicy;
        _oidcLoginConfig = oidcLoginConfig;
    }

    public async Task<bool> Valid(TokenValidatedContext context)
    {
        if (context.Principal == null) return false;
        var customerId = context.Principal.Claims.FirstOrDefault(x => x.Type == "CustomerId")?.Value;
        var email = context.Principal.Claims.FirstOrDefault(x => x.Type == "Email")?.Value;
        var passwordToken = context.Principal.Claims.FirstOrDefault(x => x.Type == "Token")?.Value;
        var refreshId = context.Principal.Claims.FirstOrDefault(x => x.Type == "RefreshId")?.Value;
        Customer customer = null;
        if (!string.IsNullOrEmpty(customerId))
        {
            //prefer the stable customer id - unambiguous with per-store identity
            customer = await _customerService.GetCustomerById(customerId);
        }
        else if (email is null)
        {
            //guest
            var id = context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Guid")?.Value;
            if (id != null) customer = await _customerService.GetCustomerByGuid(Guid.Parse(id));
        }
        else
        {
            //legacy fallback for tokens issued before the id claim existed
            customer = await _customerService.GetCustomerByEmail(email);
        }

        if (customer is null)
        {
            _errorMessage = "Not found customer";
            return false;
        }

        if (!customer.Active || customer.Deleted)
        {
            _errorMessage = "Customer not exists/or not active in the customer table";
            return false;
        }

        //provider-only: a token obtained with the password before the switch stops working at once, not at its
        //expiry. Guests never had a password: their tokens stay
        if (_oidcLoginConfig.ProviderOnly && !string.IsNullOrEmpty(customer.Email) &&
            !(await _externalSignInPolicy.PasswordSignIn(customer, SignInSurface.FrontendApi)).Allowed)
        {
            _errorMessage = "Sign in through the identity provider: tokens obtained with the password are not accepted";
            return false;
        }

        var refreshToken = await _refreshTokenService.GetCustomerRefreshToken(customer);
        if (refreshToken is null || string.IsNullOrEmpty(refreshId) || !refreshId.Equals(refreshToken.RefreshId))
        {
            _errorMessage = "Invalid token or cancel by refresh token";
            return false;
        }

        if (!await _permissionService.Authorize(StandardPermission.AllowUseApi, customer))
        {
            _errorMessage = "You do not have permission to use API operation (Customer group)";
            return false;
        }

        var customerPasswordToken = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordToken);
        var isGuest = await _groupService.IsGuest(customer);
        if (isGuest || (!string.IsNullOrEmpty(passwordToken) && passwordToken.Equals(customerPasswordToken)))
            return true;

        _errorMessage = "Invalid password token, create new token";
        return false;
    }


    public Task<string> ErrorMessage()
    {
        return Task.FromResult(_errorMessage);
    }
}