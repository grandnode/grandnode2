using Grand.Business.Authentication.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Security;
using Grand.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IUserFieldService _userFieldService;
        private readonly GrandWebApiConfig _grandWebApiConfig;

        public RefreshTokenService(IUserFieldService userFieldService, GrandWebApiConfig grandWebApiConfig)
        {
            _userFieldService = userFieldService;
            _grandWebApiConfig = grandWebApiConfig;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public async Task<RefreshToken> SaveRefreshTokenToCustomer(Customer customer, string refreshToken)
        {
            var token = new RefreshToken() {
                RefreshId=Guid.NewGuid().ToString(),
                Token = refreshToken,
                IsActive = true,
                ValidTo = DateTime.UtcNow.AddMinutes(_grandWebApiConfig.RefreshTokenExpiryInMinutes)
            };
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.RefreshToken, token);
            return token;
        }

        public async Task<RefreshToken> GetCustomerRefreshToken(Customer customer)
        {
            return await _userFieldService.GetFieldsForEntity<RefreshToken>(customer, SystemCustomerFieldNames.RefreshToken);
        }

        public ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters {
                ValidateAudience = _grandWebApiConfig.ValidateAudience, 
                ValidateIssuer = _grandWebApiConfig.ValidateIssuer,
                ValidateIssuerSigningKey = _grandWebApiConfig.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_grandWebApiConfig.SecretKey)),
                ValidateLifetime = _grandWebApiConfig.ValidateLifetime 
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}
