using Grand.Business.Core.Interfaces.Authentication;
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
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var token = form["logout_token"].ToString();
        if (string.IsNullOrEmpty(token))
            return Error("The logout_token parameter is missing");

        if (await services.GetRequiredService<IAuthenticationSchemeProvider>()
                .GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme) == null)
            return Error("OpenID Connect sign-in is not configured");

        var options = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OidcLoginDefaults.AuthenticationScheme);
        Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration configuration;
        try
        {
            configuration = await options.ConfigurationManager!.GetConfigurationAsync(context.RequestAborted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Back-channel logout: the identity provider metadata could not be obtained");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var clockSkew = TimeSpan.FromSeconds(services.GetRequiredService<OidcResourceServerConfig>().ClockSkewSeconds);
        var logout = await services.GetRequiredService<OidcLogoutTokenValidator>().Validate(token, configuration,
            services.GetRequiredService<OidcLoginConfig>().ClientId, clockSkew);
        if (!logout.IsValid)
        {
            logger.LogWarning("Back-channel logout refused: {Reason}", logout.Error);
            return Error(logout.Error);
        }

        var sessionService = services.GetRequiredService<IOidcSessionService>();
        var ended = logout.Sid != null
            ? await sessionService.EndBySid(logout.Subject, logout.Sid)
            : await sessionService.EndBySubject(logout.Subject);

        //a logout token with sid only names the subject through the sessions it ended
        var subjects = logout.Subject != null
            ? [logout.Subject]
            : ended.Select(x => x.Subject).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var revocation = services.GetRequiredService<IOidcTokenRevocationService>();
        foreach (var subject in subjects)
            await revocation.RevokeTokens(subject);

        logger.LogInformation("Back-channel logout: {Count} session(s) ended for subject {Subject}, session {Sid}",
            ended.Count, logout.Subject, logout.Sid);
        return Results.Ok();
    }

    private static IResult Error(string description) =>
        Results.Json(new Dictionary<string, string> {
            ["error"] = "invalid_request", ["error_description"] = description
        }, statusCode: StatusCodes.Status400BadRequest);
}
