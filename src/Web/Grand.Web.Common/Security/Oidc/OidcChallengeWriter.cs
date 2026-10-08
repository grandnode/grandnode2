using Grand.Business.Core.Utilities.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     RFC 6750 / RFC 9728 challenge: status, WWW-Authenticate and a ProblemDetails body
/// </summary>
public static class OidcChallengeWriter
{
    public static async Task Write(HttpContext context, OidcAuthenticationFailure failure, string resourceMetadataUrl,
        string scope)
    {
        var status = failure?.Kind is OidcFailureKind.Forbidden or OidcFailureKind.InsufficientScope
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;

        if (failure?.Kind != OidcFailureKind.Forbidden)
            context.Response.Headers[HeaderNames.WWWAuthenticate] = BuildHeader(failure, resourceMetadataUrl, scope);

        context.Response.StatusCode = status;
        var problem = new ProblemDetails {
            Status = status,
            Title = status == StatusCodes.Status401Unauthorized ? "Authentication failed" : "Access denied",
            Detail = failure?.Description
        };

        var problemDetailsService = context.RequestServices.GetService<IProblemDetailsService>();
        if (problemDetailsService != null &&
            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem }))
            return;

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
        foreach (var c in value)
        {
            if (c < 0x20 || c >= 0x7f) continue;
            if (c is '"' or '\\') builder.Append('\\');
            builder.Append(c);
        }
        return builder.ToString();
    }
}
