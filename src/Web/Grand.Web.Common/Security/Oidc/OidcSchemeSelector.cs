using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
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

    public string Select(HttpContext context)
    {
        if (_registry.Match(context.Request.Path) == null)
            return null;

        return IsOidcRequest(context.Request) ? OidcResourceServerDefaults.AuthenticationScheme : null;
    }

    /// <summary>
    ///     True when the request carries no readable bearer JWT, or one signed with an asymmetric algorithm
    /// </summary>
    public static bool IsOidcRequest(HttpRequest request)
    {
        var token = ReadBearerToken(request);
        if (token == null)
            return true;

        //identity providers sign asymmetrically; the tokens GrandNode issues itself are HMAC (HS*)
        return IsAsymmetric(token);
    }

    private static bool IsAsymmetric(string token)
    {
        try
        {
            var algorithm = new JsonWebToken(token).Alg;
            return algorithm != null && (algorithm.StartsWith("RS", StringComparison.Ordinal) ||
                                         algorithm.StartsWith("PS", StringComparison.Ordinal) ||
                                         algorithm.StartsWith("ES", StringComparison.Ordinal));
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string ReadBearerToken(HttpRequest request)
    {
        string authorization = request.Headers[HeaderNames.Authorization];
        if (string.IsNullOrEmpty(authorization) ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorization["Bearer ".Length..].Trim();
        try
        {
            //CanReadToken accepts any three segments ("x.y.z"); only a value that parses as a JWT counts as a token
            if (token.Length == 0 || !new JsonWebTokenHandler().CanReadToken(token))
                return null;
            _ = new JsonWebToken(token);
            return token;
        }
        catch (Exception)
        {
            //attacker-controlled input read for routing only: never a 500
            return null;
        }
    }
}
