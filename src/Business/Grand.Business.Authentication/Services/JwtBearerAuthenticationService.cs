using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;

#pragma warning disable CS0618 //API users stay supported until removed

namespace Grand.Business.Authentication.Services;

public class JwtBearerAuthenticationService : IJwtBearerAuthenticationService
{
    private readonly ICustomerService _customerService;
    private readonly IUserApiService _userApiService;
    private readonly OidcLoginConfig _oidcLoginConfig;
    private string _email;

    private string _errorMessage;

    public JwtBearerAuthenticationService(
        ICustomerService customerService, IUserApiService userApiService, OidcLoginConfig oidcLoginConfig)
    {
        _customerService = customerService;
        _userApiService = userApiService;
        _oidcLoginConfig = oidcLoginConfig;
    }

    /// <summary>
    ///     Valid
    /// </summary>
    /// <param name="context">Context</param>
    public virtual async Task<bool> Valid(TokenValidatedContext context)
    {
        if (context.Principal == null) return await Task.FromResult(false);

        //provider-only: API users are refused, tokens issued before the switch included
        if (_oidcLoginConfig.ProviderOnly)
        {
            _errorMessage = "Sign-in with API users is disabled: use access tokens from the identity provider";
            return false;
        }

        _email = context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Email")?.Value;
        var token = context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Token")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            _errorMessage = "Wrong token, change password on the customer and create token again";
            return await Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(_email))
        {
            _errorMessage = "Email not exists in the context";
            return await Task.FromResult(false);
        }

        var customer = await _customerService.GetCustomerByEmail(_email);
        if (customer is not { Active: true } || customer.Deleted)
        {
            _errorMessage = "Email not exists/or not active in the customer table";
            return await Task.FromResult(false);
        }

        var userApi = await _userApiService.GetUserByEmail(_email);
        if (userApi is not { IsActive: true })
        {
            _errorMessage = "User api not exists/or not active in the user api table";
            return await Task.FromResult(false);
        }

        if (userApi.Token == token) return await Task.FromResult(true);
        _errorMessage = "Wrong token, generate again";
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Get error message
    /// </summary>
    /// <returns></returns>
    public virtual Task<string> ErrorMessage()
    {
        return Task.FromResult(_errorMessage);
    }
}