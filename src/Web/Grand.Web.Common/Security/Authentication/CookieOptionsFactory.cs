using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Security.Authentication;

/// <summary>
/// Factory for creating secure cookie options
/// </summary>
public class CookieOptionsFactory(SecurityConfig securityConfig) : ICookieOptionsFactory
{
    public string CookiePrefix => securityConfig.CookiePrefix;

    /// <summary>
    /// Creates cookie options with proper security settings
    /// </summary>
    /// <param name="expiresDate">Optional explicit expiration date</param>
    /// <returns>Configured cookie options</returns>
    public CookieOptions Create(DateTime? expiresDate = null)
    {
        var cookieExpiresDate = expiresDate ?? DateTime.UtcNow.AddHours(securityConfig.CookieAuthExpires);

        var options = new CookieOptions {
            HttpOnly = true,
            Expires = cookieExpiresDate,
            Secure = securityConfig.CookieSecurePolicyAlways,
            SameSite = securityConfig.CookieSameSite,
        };

        return options;
    }
}
