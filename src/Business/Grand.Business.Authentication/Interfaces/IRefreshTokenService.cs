using Grand.Domain.Customers;
using Grand.Domain.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Interfaces
{
    public interface IRefreshTokenService
    {
        string GenerateRefreshToken();
        Task<RefreshToken> SaveRefreshTokenToCustomer(Customer customer, string refreshToken);
        Task<RefreshToken> GetCustomerRefreshToken(Customer customer);
        ClaimsPrincipal GetPrincipalFromToken(string token);
    }
}
