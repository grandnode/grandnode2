using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Authentication.OpenIdConnect.Infrastructure;

/// <summary>
///     OpenID Connect Back-Channel Logout 1.0: the identity provider tells the store that a provider session ended; the
///     store ends its sessions of that provider session (or subject) and refuses the subject's older access tokens
/// </summary>
public static class OidcBackChannelLogoutEndpoint
{
    public static async Task<IResult> Handle(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        var services = context.RequestServices;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(OidcBackChannelLogoutEndpoint));

        if (!context.Request.HasFormContentType)
            return Error("The request must be a form post");
        IFormCollection form;
        try
        {
            form = await context.Request.ReadFormAsync(context.RequestAborted);
        }
        catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException or IOException)
        {
            return Error("The request body could not be read");
        }
        var token = form["logout_token"].ToString();
        if (string.IsNullOrEmpty(token))
            return Error("The logout_token parameter is missing");

        if (await services.GetRequiredService<IAuthenticationSchemeProvider>()
                .GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme) == null)
            return Error("OpenID Connect sign-in is not configured");

        var options = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OidcLoginDefaults.AuthenticationScheme);
        var configuration = await Configuration(options, context, logger);
        if (configuration == null)
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var validator = services.GetRequiredService<OidcLogoutTokenValidator>();
        var clientId = services.GetRequiredService<OidcLoginConfig>().ClientId;
        var clockSkew = TimeSpan.FromSeconds(services.GetRequiredService<OidcResourceServerConfig>().ClockSkewSeconds);
        var logout = await validator.Validate(token, configuration, clientId, clockSkew);
        if (logout.SigningKeyNotFound)
        {
            //the provider may have rotated its signing keys since the metadata was cached, and it does not retry
            //a refused logout token: refresh the metadata and validate once more (a failed validation consumed no jti)
            logger.LogInformation("Back-channel logout: the signing key is unknown, refreshing the identity provider metadata");
            configuration = await RefreshedConfiguration(options, configuration, context, logger);
            if (configuration == null)
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            logout = await validator.Validate(token, configuration, clientId, clockSkew);
        }

        if (!logout.IsValid)
        {
            logger.LogWarning("Back-channel logout refused: {Reason}", logout.Error);
            return Error(logout.Error);
        }

        try
        {
            //the sessions were started at the provider the links belong to: while that is not the configured one, a
            //subject or sid of the configured provider may name another person's session - the subject's tokens are
            //still refused (a token is checked against the subject of its own issuer)
            IList<OidcSession> ended = [];
            if (await services.GetRequiredService<IOidcLinkService>().LinksMatchProvider())
            {
                var sessionService = services.GetRequiredService<IOidcSessionService>();
                ended = logout.Sid != null
                    ? await sessionService.EndBySid(logout.Subject, logout.Sid)
                    : await sessionService.EndBySubject(logout.Subject);
            }
            else
                logger.LogWarning("Back-channel logout: no session ended, the identity provider changed and the account " +
                                  "links are not reviewed yet");

            //a logout token with sid only names the subject through the sessions it ended
            var subjects = logout.Subject != null
                ? [logout.Subject]
                : ended.Select(x => x.Subject).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            var revocation = services.GetRequiredService<IOidcTokenRevocationService>();
            foreach (var subject in subjects)
                await revocation.RevokeTokens(subject);

            logger.LogInformation("Back-channel logout: {Count} session(s) ended for subject {Subject}, session {Sid}",
                ended.Count, logout.Subject, logout.Sid);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            //the provider retries a failed request: the same logout token must be accepted again
            validator.Release(logout.ReplayKey);
            logger.LogError(ex, "Back-channel logout: ending the sessions failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Results.Ok();
    }

    private static async Task<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration> Configuration(
        OpenIdConnectOptions options, HttpContext context, ILogger logger)
    {
        try
        {
            return await options.ConfigurationManager!.GetConfigurationAsync(context.RequestAborted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Back-channel logout: the identity provider metadata could not be obtained");
            return null;
        }
    }

    /// <summary>
    ///     The configuration manager refreshes in the background and answers with the cached metadata meanwhile, so
    ///     wait (briefly) for a new instance; a refresh requested too soon after the last one is ignored by the manager
    /// </summary>
    private static async Task<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration> RefreshedConfiguration(
        OpenIdConnectOptions options, Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration cached,
        HttpContext context, ILogger logger)
    {
        options.ConfigurationManager!.RequestRefresh();
        for (var attempt = 0;; attempt++)
        {
            var configuration = await Configuration(options, context, logger);
            if (configuration == null || !ReferenceEquals(configuration, cached) || attempt == RefreshAttempts)
                return configuration;
            await Task.Delay(RefreshPollInterval, context.RequestAborted);
        }
    }

    private const int RefreshAttempts = 10;
    private static readonly TimeSpan RefreshPollInterval = TimeSpan.FromMilliseconds(200);

    private static IResult Error(string description) =>
        Results.Json(new Dictionary<string, string> {
            ["error"] = "invalid_request", ["error_description"] = description
        }, statusCode: StatusCodes.Status400BadRequest);
}
