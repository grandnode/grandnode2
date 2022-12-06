﻿using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Grand.Business.Authentication.Services
{
    public class ApiAuthenticationService : IApiAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private Customer _cachedCustomer;


        public ApiAuthenticationService(
            IHttpContextAccessor httpContextAccessor,
            ICustomerService customerService,
            IGroupService groupService)
        {
            _httpContextAccessor = httpContextAccessor;
            _customerService = customerService;
            _groupService = groupService;
        }

        public virtual async Task<Customer> GetAuthenticatedCustomer()
        {
            //whether there is a cached customer
            if (_cachedCustomer != null)
                return _cachedCustomer;

            Customer customer = null;
            if (_httpContextAccessor.HttpContext == null) return null;
            
            //try to get authenticated user identity
            string authHeader = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Authorization];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(JwtBearerDefaults.AuthenticationScheme))
                return null;

            if (_httpContextAccessor.HttpContext.Request.Path.Value != null 
                && !_httpContextAccessor.HttpContext.Request.Path.Value.StartsWith("/odata"))
            {
                customer = await ApiCustomer();
                if (customer == null) return null;
                _cachedCustomer = customer;
                return _cachedCustomer;
            }

            var authenticateResult =
                await _httpContextAccessor.HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
                return null;

            //try to get customer by email
            var emailClaim = authenticateResult.Principal.Claims.FirstOrDefault(claim => claim.Type == "Email");
            if (emailClaim != null)
                customer = await _customerService.GetCustomerByEmail(emailClaim.Value);

            //whether the found customer is available
            if (customer is not { Active: true } || customer.Deleted || !await _groupService.IsRegistered(customer))
                return null;

            //cache authenticated customer
            _cachedCustomer = customer;

            return _cachedCustomer;
        }

        private async Task<Customer> ApiCustomer()
        {
            Customer customer = null;
            if (_httpContextAccessor.HttpContext == null) return null;
            var authResult = await _httpContextAccessor.HttpContext.AuthenticateAsync(FrontendAPIConfig.Scheme);
            if (!authResult.Succeeded)
            {
                _httpContextAccessor.HttpContext.Response.StatusCode = 400;
                _httpContextAccessor.HttpContext.Response.ContentType = "text/plain";
                if (authResult.Failure != null)
                    await _httpContextAccessor.HttpContext.Response.WriteAsync(authResult.Failure.Message);
                return await _customerService.GetCustomerBySystemName(SystemCustomerNames.Anonymous);
            }
            var email = authResult.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Email")?.Value;
            if (email is null)
            {
                //guest
                var id = authResult.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Guid")?.Value;
                if (id != null) customer = await _customerService.GetCustomerByGuid(Guid.Parse(id));
            }
            else
            {
                customer = await _customerService.GetCustomerByEmail(email);
            }
            return customer;
        }
    }
}