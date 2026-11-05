using System.Security.Claims;

namespace Grand.Business.Core.Interfaces.Authentication;

[Obsolete("API users with a shared SecretKey are deprecated - configure OidcResourceServer and use access tokens of your identity provider (docs/oidc-resource-server.md)")]
public interface IJwtBearerAuthenticationService
{
    /// <summary>
    ///     Valid email
    /// </summary>
    /// <param name="principal">Principal of the validated token</param>
    Task<bool> Valid(ClaimsPrincipal principal);

    /// <summary>
    ///     Get error message
    /// </summary>
    /// <returns></returns>
    Task<string> ErrorMessage();
}