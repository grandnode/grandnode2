using Grand.Business.Authentication.Interfaces;
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
        private string _errorMessage;
        public JwtBearerCustomerAuthenticationService(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<(bool, Customer)> Valid(TokenValidatedContext context)
        {
            var email = context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Email")?.Value;
            Customer customer = null;
            if(email is null)
            {
                //guest
                var id= context.Principal.Claims.ToList().FirstOrDefault(x => x.Type == "Id")?.Value;
                customer = await _customerService.GetCustomerById(id);
            }
            else
            {
                customer = await _customerService.GetCustomerByEmail(email);
            }
            if (customer is null)
            {
                _errorMessage = "Not found customer";
                return (false, null);
            }

            if (customer == null || !customer.Active || customer.Deleted)
            {
                _errorMessage = "Customer not exists/or not active in the customer table";
                return (false, null);
            }

            return (true, customer);
        }


        public Task<string> ErrorMessage()
        {
            return Task.FromResult(_errorMessage);
        }

    }
}
