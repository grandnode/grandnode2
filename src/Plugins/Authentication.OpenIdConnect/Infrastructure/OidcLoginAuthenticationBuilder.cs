using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Authentication.OpenIdConnect.Infrastructure;

/// <summary>
///     Browser sign-in with the identity provider of the resource server (authorization code + PKCE)
/// </summary>
public class OidcLoginAuthenticationBuilder : IAuthenticationBuilder
{
    public void AddAuthentication(AuthenticationBuilder builder, IConfiguration configuration)
    {
        var resourceServer = new OidcResourceServerConfig();
        configuration.GetSection("OidcResourceServer").Bind(resourceServer);
        var login = new OidcLoginConfig();
        configuration.GetSection(OidcLoginDefaults.ConfigSection).Bind(login);

        //not configured: no scheme; the login action answers with the failure page
        if (!login.IsConfigured(resourceServer))
            return;

        builder.AddOpenIdConnect(OidcLoginDefaults.AuthenticationScheme, options =>
        {
            options.Authority = resourceServer.Authority;
            options.RequireHttpsMetadata = resourceServer.RequireHttpsMetadata;
            options.ClientId = login.ClientId;
            options.ClientSecret = login.ClientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.ResponseMode = OpenIdConnectResponseMode.Query;
            options.UsePkce = true;
            options.CallbackPath = OidcLoginDefaults.CallbackPath;
            options.SaveTokens = false;
            options.MapInboundClaims = false;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.Scope.Clear();
            foreach (var scope in OidcLoginIdentityMapper.Scopes(login.Scopes))
                options.Scope.Add(scope);
            //user info answers are mapped only for the keys listed here
            foreach (var claim in new[] { resourceServer.Claims.Subject, resourceServer.Claims.Email,
                         resourceServer.Claims.EmailVerified, "given_name", "family_name" })
                options.ClaimActions.MapUniqueJsonKey(claim, claim);

            options.Events = new OpenIdConnectEvents {
                //the refresh token stays out of the ticket's tokens (SaveTokens is off): only the server-side session keeps it
                OnTokenValidated = ctx =>
                {
                    OidcLoginSessionData.Capture(ctx.Properties, ctx.TokenEndpointResponse);
                    return Task.CompletedTask;
                },
                OnTicketReceived = async ctx =>
                {
                    var identity = OidcLoginIdentityMapper.Map(ctx.Principal, resourceServer.Claims);
                    if (identity == null)
                    {
                        ctx.HandleResponse();
                        Logger(ctx.HttpContext).LogWarning("OpenID Connect sign-in without a subject or an e-mail");
                        await Fail(ctx.HttpContext);
                        return;
                    }

                    //only what the sign-in needs, kept briefly in the external authentication cookie
                    ctx.Principal = ExternalRegistrationContext.CreatePrincipal(identity);
                    OidcLoginIdentityMapper.ApplyPendingLifetime(ctx.Properties!, DateTimeOffset.UtcNow);
                },
                OnRemoteFailure = async ctx =>
                {
                    ctx.HandleResponse();
                    Logger(ctx.HttpContext).LogWarning(ctx.Failure, "OpenID Connect sign-in failed");
                    await Fail(ctx.HttpContext);
                }
            };
        });
    }

    //a pending identity of an earlier attempt must not outlive a failed one: the next registration or link would use it
    private static async Task Fail(Microsoft.AspNetCore.Http.HttpContext context)
    {
        await context.SignOutAsync(GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme);
        context.Response.Redirect(context.Request.PathBase + OidcLoginDefaults.FailedPath);
    }

    private static ILogger Logger(Microsoft.AspNetCore.Http.HttpContext context) =>
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(OidcLoginAuthenticationBuilder));

    public int Priority => 503;
}
