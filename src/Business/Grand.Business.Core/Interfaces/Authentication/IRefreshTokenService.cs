using Grand.Domain.Customers;
using Grand.Domain.Security;
using System.Security.Claims;

namespace Grand.Business.Core.Interfaces.Authentication
{
    public interface IRefreshTokenService
    {
        string GenerateRefreshToken();
        Task<RefreshToken> SaveRefreshTokenToCustomer(Customer customer, string refreshToken);
        Task<RefreshToken> GetCustomerRefreshToken(Customer customer);
        ClaimsPrincipal GetPrincipalFromToken(string token);
    }
}
