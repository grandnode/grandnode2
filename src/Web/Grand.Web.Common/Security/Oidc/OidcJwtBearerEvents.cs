using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Oidc scheme events: per-resource checks after signature validation, RFC 6750 answers on failure
/// </summary>
public class OidcJwtBearerEvents : JwtBearerEvents
{
    protected readonly OidcResourceServerConfig _config;

    public OidcJwtBearerEvents(OidcResourceServerConfig config)
    {
        _config = config;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        var http = context.HttpContext;
        var registry = http.RequestServices.GetRequiredService<IOidcProtectedResourceRegistry>();
        var resource = registry.Match(http.Request.Path);
        if (resource == null)
        {
            Reject(context, OidcFailureKind.InvalidToken, "The token is not accepted on this path");
            return;
        }

        var canonical = OidcCanonicalUri.ResourceFor(resource, await OidcCanonicalUri.StoreBaseUrl(http));
        if (!OidcTokenChecks.HasAudience(principal, canonical, resource.Audiences))
        {
            Reject(context, OidcFailureKind.InvalidToken, "The token was issued for another audience");
            return;
        }

        if (!OidcTokenChecks.HasAuthorizedParty(principal, resource.AuthorizedParties))
        {
            Reject(context, OidcFailureKind.InvalidToken, "The token was issued to a client that is not allowed");
            return;
        }

        //checked before any customer lookup: a token without the scope never registers an account
        if (OidcTokenChecks.MissingScopes(principal, resource.RequiredScopes).Count > 0)
        {
            Reject(context, OidcFailureKind.InsufficientScope, "The token lacks a required scope",
                string.Join(' ', OidcTokenChecks.PublicScopes(resource.RequiredScopes)));
            return;
        }

        http.Items[OidcResourceServerDefaults.ResourceItemKey] = resource;

        if (!await AfterTokenChecks(context, resource) && OidcAuthenticationFailure.Get(http) == null)
            Reject(context, OidcFailureKind.InvalidToken, "The token is not accepted");
    }

    /// <summary>
    ///     Runs after the resource, audience, party and scope checks; calls Reject and returns false to refuse.
    ///     The base refuses resources with account conditions: checking them needs the customer an override resolves.
    /// </summary>
    protected virtual Task<bool> AfterTokenChecks(TokenValidatedContext context, OidcProtectedResource resource)
    {
        if (!resource.RequireAdminPanelAccess && !resource.DenyVendors && !resource.DenyStoreManagers)
            return Task.FromResult(true);

        Reject(context, OidcFailureKind.Forbidden, "Account conditions for this resource are not available");
        return Task.FromResult(false);
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        var description = context.Exception switch {
            SecurityTokenExpiredException => "The token has expired",
            SecurityTokenNotYetValidException => "The token is not valid yet",
            SecurityTokenInvalidIssuerException => "The token was issued by another issuer",
            SecurityTokenInvalidAlgorithmException => "The token signing algorithm is not accepted",
            SecurityTokenSignatureKeyNotFoundException or SecurityTokenInvalidSignatureException =>
                "The token signature is invalid",
            _ => "The token is invalid"
        };
        Logger(context.HttpContext).LogInformation("Access token rejected: {Reason} ({Exception})", description,
            context.Exception.GetType().Name);
        OidcAuthenticationFailure.Set(context.HttpContext,
            new OidcAuthenticationFailure { Kind = OidcFailureKind.InvalidToken, Description = description });
        return Task.CompletedTask;
    }

    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var http = context.HttpContext;
        var failure = OidcAuthenticationFailure.Get(http);
        if (failure == null && context.AuthenticateFailure != null)
            failure = new OidcAuthenticationFailure { Kind = OidcFailureKind.InvalidToken, Description = "The token is invalid" };

        var resource = http.RequestServices.GetRequiredService<IOidcProtectedResourceRegistry>().Match(http.Request.Path);
        var metadataUrl = resource != null
            ? OidcProtectedResourceMetadata.MetadataUrl(
                OidcCanonicalUri.ResourceFor(resource, await OidcCanonicalUri.StoreBaseUrl(http)))
            : null;
        var scope = resource != null ? string.Join(' ', OidcTokenChecks.PublicScopes(resource.RequiredScopes)) : null;
        await OidcChallengeWriter.Write(http, failure, metadataUrl, scope);
    }

    protected void Reject(TokenValidatedContext context, OidcFailureKind kind, string description, string scope = null)
    {
        var principal = context.Principal;
        Logger(context.HttpContext).LogWarning("Access token refused for subject {Subject} of {Issuer}: {Reason}",
            principal?.FindFirst(_config.Claims.Subject)?.Value, principal?.FindFirst("iss")?.Value, description);
        OidcAuthenticationFailure.Set(context.HttpContext,
            new OidcAuthenticationFailure { Kind = kind, Description = description, Scope = scope });
        context.Fail(description);
    }

    protected static ILogger Logger(HttpContext context) =>
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Grand.Oidc");
}
