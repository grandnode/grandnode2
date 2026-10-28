using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Grand.Module.Scim.Infrastructure;

/// <summary>
///     Lets in only the identity provider: the Bearer token of Scim:Token, compared in constant time. With a token shorter than
///     32 characters, or without the OpenID Connect sign-in, every request is refused
/// </summary>
public class ScimAuthorizeFilter(
    ScimConfig config,
    OidcLoginConfig loginConfig,
    OidcResourceServerConfig resourceServerConfig,
    ILogger<ScimAuthorizeFilter> logger) : IAuthorizationFilter
{
    private const string Scheme = "Bearer ";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!config.TokenUsable || !loginConfig.IsConfigured(resourceServerConfig))
        {
            logger.LogWarning("SCIM request refused: Scim:Token is shorter than 32 characters or the OpenID Connect sign-in " +
                              "(OidcLogin) is not configured");
            Refuse(context);
            return;
        }

        string authorization = context.HttpContext.Request.Headers.Authorization;
        if (authorization == null || !authorization.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase) ||
            !Matches(authorization[Scheme.Length..].Trim(), config.Token))
        {
            logger.LogWarning("SCIM request refused: missing or wrong Bearer token");
            Refuse(context);
        }
    }

    //hashing first gives both sides one length, so the comparison time tells nothing about the token
    private static bool Matches(string presented, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(presented)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));

    private static void Refuse(AuthorizationFilterContext context)
    {
        context.HttpContext.Response.Headers.WWWAuthenticate = "Bearer";
        context.Result = ScimResponse.Error(StatusCodes.Status401Unauthorized, null, "Authentication required");
    }
}
