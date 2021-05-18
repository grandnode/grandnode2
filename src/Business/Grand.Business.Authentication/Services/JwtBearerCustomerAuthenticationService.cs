using Grand.Business.Authentication.Interfaces;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Customers;
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
        private string _errorMessage;

        public JwtBearerCustomerAuthenticationService(ICustomerService customerService,IPermissionService permissionService)
        {
            _customerService = customerService;
            _permissionService = permissionService;
        }

        public async Task<bool> Valid(TokenValidatedContext context)
        {
            var email = context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Email")?.Value;
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
            if(!(await _permissionService.Authorize(StandardPermission.AllowUseApi,customer)))
            {
                _errorMessage = "Customer not has perrmission";
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
