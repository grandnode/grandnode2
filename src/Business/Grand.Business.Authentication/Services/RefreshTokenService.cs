using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Security;
using Grand.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Grand.Business.Authentication.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly FrontendAPIConfig _apiConfig;
    private readonly ICustomerService _customerService;

    public RefreshTokenService(ICustomerService customerService, FrontendAPIConfig apiConfig)
    {
        _customerService = customerService;
        _apiConfig = apiConfig;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<RefreshToken> SaveRefreshTokenToCustomer(Customer customer, string refreshToken)
    {
        var token = new RefreshToken {
            RefreshId = Guid.NewGuid().ToString(),
            Token = refreshToken,
            IsActive = true,
            ValidTo = DateTime.UtcNow.AddMinutes(_apiConfig.RefreshTokenExpiryInMinutes)
        };
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.RefreshToken, token);
        return token;
    }

    public Task<RefreshToken> GetCustomerRefreshToken(Customer customer)
    {
        return Task.FromResult(customer.GetUserFieldFromEntity<RefreshToken>(SystemCustomerFieldNames.RefreshToken));
    }

    public ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters {
            ValidateAudience = _apiConfig.ValidateAudience,
            ValidateIssuer = _apiConfig.ValidateIssuer,
            ValidateIssuerSigningKey = _apiConfig.ValidateIssuerSigningKey,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_apiConfig.SecretKey)),
            ValidateLifetime = _apiConfig.ValidateLifetime
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}