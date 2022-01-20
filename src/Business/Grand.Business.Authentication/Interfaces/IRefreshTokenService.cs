using Grand.Domain.Customers;
using Grand.Domain.Security;
using System.Security.Claims;

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
