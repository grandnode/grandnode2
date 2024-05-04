using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Grand.Business.Authentication.Services;

public class JwtBearerAuthenticationService : IJwtBearerAuthenticationService
{
    private readonly ICustomerService _customerService;
    private readonly IUserApiService _userApiService;
    private string _email;

    private string _errorMessage;

    public JwtBearerAuthenticationService(
        ICustomerService customerService, IUserApiService userApiService)
    {
        _customerService = customerService;
        _userApiService = userApiService;
    }

    /// <summary>
    ///     Valid
    /// </summary>
    /// <param name="context">Context</param>
    public virtual async Task<bool> Valid(TokenValidatedContext context)
    {
        if (context.Principal == null) return await Task.FromResult(false);

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