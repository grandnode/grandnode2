using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
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

        //JsonWebTokenHandler is the default handler: IssuedAt is MinValue when the token has no iat
        var issuedUtc = (context.SecurityToken as JsonWebToken)?.IssuedAt ?? DateTime.MinValue;
        if (OidcTokenChecks.IsTooOld(issuedUtc, resource.MaxTokenAgeMinutes, _config.ClockSkewSeconds, DateTime.UtcNow))
        {
            Reject(context, OidcFailureKind.InvalidToken, "The token is too old");
            return;
        }

        //the provider ended the subject's session (back-channel logout) or refused the account after this token was issued
        var subject = principal.FindFirst(_config.Claims.Subject)?.Value;
        if (await http.RequestServices.GetRequiredService<IOidcTokenRevocationService>().IsRevoked(subject, issuedUtc))
        {
            Reject(context, OidcFailureKind.InvalidToken, "The token was revoked");
            return;
        }

        http.Items[OidcResourceServerDefaults.ResourceItemKey] = resource;

        if (!await AfterTokenChecks(context, resource) && OidcAuthenticationFailure.Get(http) == null)
            Reject(context, OidcFailureKind.InvalidToken, "The token is not accepted");
    }

    /// <summary>
    ///     Runs after the resource, audience, party and scope checks: maps the token to a customer, applies the
    ///     resource's account conditions and hands the customer to the request. Calls Reject and returns false to refuse.
    /// </summary>
    protected virtual async Task<bool> AfterTokenChecks(TokenValidatedContext context, OidcProtectedResource resource)
    {
        var http = context.HttpContext;
        var services = http.RequestServices;
        //the store whose URL the audience was checked against (from the host, never the store cookie)
        var storeId = (await OidcCanonicalUri.RequestStore(http))?.Id ?? "";
        var result = await services.GetRequiredService<IOidcCustomerResolver>().Resolve(context.Principal!, storeId);
        if (!result.Succeeded)
        {
            Reject(context, result.Failure ?? OidcFailureKind.InvalidToken, result.Description);
            return false;
        }

        var refusal = await services.GetRequiredService<OidcResourceAccess>().Refusal(resource, result.Customer);
        if (refusal != null)
        {
            Reject(context, OidcFailureKind.Forbidden, refusal);
            return false;
        }

        http.Items[OidcResourceServerDefaults.CustomerItemKey] = result.Customer;
        //the work context is built in the same store: prices, settings and per-store checks follow the token, not a
        //store cookie the client also sends (authenticated before ContextMiddleware, see OidcAuthenticationMiddleware)
        RequestStorePin.Set(http, storeId);
        return true;
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        if (ConfigurationUnavailable(context))
        {
            Logger(context.HttpContext).LogError(context.Exception,
                "Access token not validated: the authorization server metadata or signing keys could not be obtained from {MetadataAddress}",
                context.Options.MetadataAddress);
            OidcAuthenticationFailure.Set(context.HttpContext, new OidcAuthenticationFailure {
                Kind = OidcFailureKind.ServiceUnavailable, Description = "The authorization server is unavailable"
            });
            //a result stops JwtBearer from rethrowing a retrieval exception it caught itself
            context.Fail(context.Exception);
            return Task.CompletedTask;
        }

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

    /// <summary>
    ///     The failure comes from the authorization server's metadata or keys being unobtainable, not from the token:
    ///     the configuration manager reports it, or the exception is the retrieval failure itself (IDX20803, or a
    ///     network error inside)
    /// </summary>
    private static bool ConfigurationUnavailable(AuthenticationFailedContext context)
    {
        if (context.Options.ConfigurationManager is OidcConfigurationManager { Unavailable: true })
            return true;

        if (context.Exception is InvalidOperationException invalid && invalid.Message.Contains("IDX20803"))
            return true;
        for (var exception = context.Exception; exception != null; exception = exception.InnerException)
            if (exception is HttpRequestException)
                return true;
        return false;
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
