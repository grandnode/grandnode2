using Grand.Business.Core.Utilities.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     RFC 6750 / RFC 9728 challenge: status, WWW-Authenticate and a ProblemDetails body
/// </summary>
public static class OidcChallengeWriter
{
    /// <summary>Retry-After of the 503 answered while the authorization server is unreachable</summary>
    public const int RetryAfterSeconds = 30;

    public static async Task Write(HttpContext context, OidcAuthenticationFailure failure, string resourceMetadataUrl,
        string scope)
    {
        var status = failure?.Kind switch {
            OidcFailureKind.Forbidden or OidcFailureKind.InsufficientScope => StatusCodes.Status403Forbidden,
            OidcFailureKind.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status401Unauthorized
        };

        //no bearer error when the token was not at fault: a refusal of the account, or no authorization server
        if (failure?.Kind is not (OidcFailureKind.Forbidden or OidcFailureKind.ServiceUnavailable))
            context.Response.Headers[HeaderNames.WWWAuthenticate] = BuildHeader(failure, resourceMetadataUrl, scope);

        //RFC 9110 section 10.2.3: a hint when to try again; the identity provider is retried on later requests
        if (status == StatusCodes.Status503ServiceUnavailable)
            context.Response.Headers[HeaderNames.RetryAfter] = RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        context.Response.StatusCode = status;
        var problem = new ProblemDetails {
            Status = status,
            Title = status switch {
                StatusCodes.Status401Unauthorized => "Authentication failed",
                StatusCodes.Status503ServiceUnavailable => "Service unavailable",
                _ => "Access denied"
            },
            Detail = failure?.Description
        };

        //written directly: IProblemDetailsService picks MVC's writer first on controller endpoints, and that writer
        //writes nothing for controllers without [ApiController] while still reporting the body as written
        await context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions)null, "application/problem+json");
    }

    /// <param name="scope">Scopes of the initial 401 (no failure); a failure carries its own</param>
    public static string BuildHeader(OidcAuthenticationFailure failure, string resourceMetadataUrl, string scope)
    {
        var parameters = new List<string>();
        if (failure != null)
        {
            parameters.Add(failure.Kind == OidcFailureKind.InsufficientScope
                ? "error=\"insufficient_scope\""
                : "error=\"invalid_token\"");
            if (!string.IsNullOrEmpty(failure.Description))
                parameters.Add($"error_description=\"{Escape(failure.Description)}\"");
            if (!string.IsNullOrEmpty(failure.Scope))
                parameters.Add($"scope=\"{Escape(failure.Scope)}\"");
        }

        if (!string.IsNullOrEmpty(resourceMetadataUrl))
            parameters.Add($"resource_metadata=\"{Escape(resourceMetadataUrl)}\"");

        if (failure == null && !string.IsNullOrEmpty(scope))
            parameters.Add($"scope=\"{Escape(scope)}\"");

        return parameters.Count == 0 ? "Bearer" : "Bearer " + string.Join(", ", parameters);
    }

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        //printable ASCII only: control and non-ASCII characters are dropped
        foreach (var c in value.Where(c => c is >= (char)0x20 and < (char)0x7f))
        {
            if (c is '"' or '\\') builder.Append('\\');
            builder.Append(c);
        }
        return builder.ToString();
    }
}
