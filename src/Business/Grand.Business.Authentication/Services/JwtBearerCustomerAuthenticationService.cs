using Grand.Business.Authentication.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Customers;
using Grand.Domain.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Services
{
    public class JwtBearerCustomerAuthenticationService : IJwtBearerCustomerAuthenticationService
    {
        private readonly ICustomerService _customerService;
        private readonly IPermissionService _permissionService;
        private readonly IUserFieldService _userFieldService;
        private readonly IGroupService _groupService;
        private readonly IRefreshTokenService _refreshTokenService;
        private string _errorMessage;

        public JwtBearerCustomerAuthenticationService(ICustomerService customerService,IPermissionService permissionService
            ,IUserFieldService userFieldService,IGroupService groupService,IRefreshTokenService refreshTokenService)
        {
            _customerService = customerService;
            _permissionService = permissionService;
            _userFieldService = userFieldService;
            _groupService = groupService;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<bool> Valid(TokenValidatedContext context)
        {
            var email = context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Email")?.Value;
            var passwordToken = context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Token")?.Value;
            var refreshId = context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "RefreshId")?.Value;
            Customer customer = null;
            if(email is null)
            {
                //guest
                var id= context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Guid")?.Value;
                customer = await _customerService.GetCustomerByGuid(Guid.Parse(id));
            }
            else
            {
                customer = await _customerService.GetCustomerByEmail(email);
            }
            if (customer is null)
            {
                _errorMessage = "Not found customer";
                return false;
            }

            if (customer == null || !customer.Active || customer.Deleted )
            {
                _errorMessage = "Customer not exists/or not active in the customer table";
                return false;
            }

            var refreshToken = await _refreshTokenService.GetCustomerRefreshToken(customer);
            if(refreshToken is null || string.IsNullOrEmpty(refreshId) || !refreshId.Equals(refreshToken?.RefreshId))
            {
                _errorMessage = "Invalid token or cancel by refresh token";
                return false;
            }

            if (!(await _permissionService.Authorize(StandardPermission.AllowUseApi,customer)))
            {
                _errorMessage = "Customer not has permission";
                return false;
            }

            var customerPasswordtoken = await _userFieldService.GetFieldsForEntity<string>(customer, SystemCustomerFieldNames.PasswordToken);
            var isGuest = await _groupService.IsGuest(customer);
            if(!isGuest && (string.IsNullOrEmpty(passwordToken) || !passwordToken.Equals(customerPasswordtoken)))
            {
                _errorMessage = "Invalid password token, create new token";
                return false;
            }
            

            return true;
        }


        public Task<string> ErrorMessage()
        {
            return Task.FromResult(_errorMessage);
        }

    }
}
