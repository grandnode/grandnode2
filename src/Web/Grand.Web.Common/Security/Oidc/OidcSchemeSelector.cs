using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Lets the Web API's JwtBearer scheme ("Bearer") hand requests on protected resources to the Oidc
///     scheme: requests without a readable bearer JWT (so the 401 is the OIDC challenge) and requests whose token is
///     signed asymmetrically, as identity providers sign. HMAC tokens - those GrandNode signs with its SecretKey - stay
///     with the original scheme, whatever issuer they claim (Oidc accepts no HMAC algorithm). The algorithm is read
///     unvalidated, for routing only; the scheme the request lands on validates the token.
/// </summary>
public class OidcSchemeSelector : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly IOidcProtectedResourceRegistry _registry;

    public OidcSchemeSelector(IOidcProtectedResourceRegistry registry)
    {
        _registry = registry;
    }

    public void PostConfigure(string name, JwtBearerOptions options)
    {
        //registered only while the resource server is enabled (OidcAuthenticationBuilderExtensions); only the Web
        //API's scheme: other JwtBearer schemes (FrontendAPI, plugins) keep their own validation and challenge
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        var previous = options.ForwardDefaultSelector;
        options.ForwardDefaultSelector = context => Select(context) ?? previous?.Invoke(context);
    }

    /// <summary>
    ///     "Oidc" for a request on a protected resource that the Oidc scheme should authenticate, otherwise null.
    ///     Decided once per request (the selector runs for every authenticate, challenge and forbid, and the work
    ///     context asks too) and kept in HttpContext.Items with the path it was made for.
    /// </summary>
    public string Select(HttpContext context)
    {
        var path = context.Request.Path;
        if (context.Items.TryGetValue(OidcResourceServerDefaults.RouteItemKey, out var cached) &&
            cached is Route route && route.Path == path)
            return route.Scheme;

        var scheme = _registry.Match(path) != null && IsOidcRequest(context.Request)
            ? OidcResourceServerDefaults.AuthenticationScheme
            : null;
        context.Items[OidcResourceServerDefaults.RouteItemKey] = new Route(path, scheme);
        return scheme;
    }

    /// <summary>
    ///     True when the request carries no readable bearer JWT, or one signed with an asymmetric algorithm the Oidc
    ///     scheme accepts. The token is parsed once, unvalidated.
    /// </summary>
    public static bool IsOidcRequest(HttpRequest request)
    {
        string authorization = request.Headers[HeaderNames.Authorization];
        if (string.IsNullOrEmpty(authorization) ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return true;

        var token = authorization["Bearer ".Length..].Trim();
        //the size limit JwtBearer applies itself
        if (token.Length == 0 || token.Length > TokenValidationParameters.DefaultMaximumTokenSizeInBytes)
            return true;

        string algorithm;
        try
        {
            algorithm = new JsonWebToken(token).Alg;
        }
        catch (Exception ex) when (ex is ArgumentException or SecurityTokenMalformedException or FormatException)
        {
            //attacker-controlled input read for routing only: what the parser reports for a malformed value
            //(not a JWS/JWE, undecodable segments, invalid JSON) counts as no token - never a 500
            return true;
        }

        //identity providers sign asymmetrically; the tokens GrandNode issues itself are HMAC (HS*)
        return algorithm != null && OidcAuthenticationBuilderExtensions.AsymmetricAlgorithms.Contains(algorithm);
    }

    private sealed record Route(PathString Path, string Scheme);
}
