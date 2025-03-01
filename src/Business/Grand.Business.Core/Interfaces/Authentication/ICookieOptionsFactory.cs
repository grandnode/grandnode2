using Microsoft.AspNetCore.Http;

namespace Grand.Business.Core.Interfaces.Authentication;

// <summary>
/// Interface for factory creating secure cookie options
/// </summary>
public interface ICookieOptionsFactory
{
    /// <summary>
    /// Creates cookie options with proper security settings
    /// </summary>
    /// <param name="expiresDate">Optional explicit expiration date</param>
    /// <returns>Configured cookie options</returns>
    CookieOptions Create(DateTime? expiresDate = null);

    /// <summary>
    /// Gets the cookie prefix
    /// </summary>
    string CookiePrefix { get; }
}
