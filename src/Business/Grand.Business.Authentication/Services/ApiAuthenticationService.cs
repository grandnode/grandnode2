using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Attributes;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Grand.Business.Authentication.Services;

public class ApiAuthenticationService : IApiAuthenticationService
{
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuthenticationService(
        ICustomerService customerService,
        IGroupService groupService, IHttpContextAccessor httpContextAccessor)
    {
        _customerService = customerService;
        _groupService = groupService;
        _httpContextAccessor = httpContextAccessor;
    }

    public virtual async Task<Customer> GetAuthenticatedCustomer()
    {
        Customer customer = null;
        if (_httpContextAccessor.HttpContext == null) return null;

        string authHeader = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Authorization];
        if (string.IsNullOrEmpty(authHeader))
            return null;

        if (IsApiFrontAuthenticated())
        {
            customer = await ApiCustomer();
            return customer;
        }

        var authenticateResult = await _httpContextAccessor.HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return null;

        //prefer the stable customer id (unambiguous with per-store identity), fall back to e-mail for old tokens
        var customerIdClaim = authenticateResult.Principal.Claims.FirstOrDefault(claim => claim.Type == "CustomerId");
        if (customerIdClaim != null)
            customer = await _customerService.GetCustomerById(customerIdClaim.Value);
        else
        {
            var emailClaim = authenticateResult.Principal.Claims.FirstOrDefault(claim => claim.Type == "Email");
            if (emailClaim != null)
                customer = await _customerService.GetCustomerByEmail(emailClaim.Value);
        }

        //whether the found customer is available
        if (customer is not { Active: true } || customer.Deleted || !await _groupService.IsRegistered(customer))
            return null;

        return customer;
    }
    private bool IsApiFrontAuthenticated()
    {
        var endpoint = _httpContextAccessor.HttpContext.GetEndpoint();
        if (endpoint == null) return false;

        var apiGroupAttr = endpoint.Metadata.GetOrderedMetadata<ApiGroupAttribute>();
        return apiGroupAttr.Any(attr => attr.GroupName == ApiConstants.ApiGroupNameV2);
    }
    

    private async Task<Customer> ApiCustomer()
    {
        Customer customer = null;
        var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(FrontendAPIConfig.AuthenticationScheme);
        if (!authResult.Succeeded)
            return await _customerService.GetCustomerBySystemName(SystemCustomerNames.Anonymous);

        var customerId = authResult.Principal.Claims.FirstOrDefault(x => x.Type == "CustomerId")?.Value;
        var email = authResult.Principal.Claims.FirstOrDefault(x => x.Type == "Email")?.Value;
        if (!string.IsNullOrEmpty(customerId))
        {
            //prefer the stable customer id - unambiguous with per-store identity
            customer = await _customerService.GetCustomerById(customerId);
        }
        else if (email is null)
        {
            //guest
            var id = authResult.Principal.Claims.FirstOrDefault(x => x.Type == "Guid")?.Value;
            if (id != null) customer = await _customerService.GetCustomerByGuid(Guid.Parse(id));
        }
        else
            customer = await _customerService.GetCustomerByEmail(email);

        return customer;
    }
}