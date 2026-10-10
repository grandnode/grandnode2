using Grand.Business.Core.Interfaces.Authentication;
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
            foreach (var scope in login.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                options.Scope.Add(scope);
            //user info answers are mapped only for the keys listed here
            foreach (var claim in new[] { resourceServer.Claims.Subject, resourceServer.Claims.Email,
                         resourceServer.Claims.EmailVerified, "given_name", "family_name" })
                options.ClaimActions.MapUniqueJsonKey(claim, claim);

            options.Events = new OpenIdConnectEvents {
                OnTicketReceived = ctx =>
                {
                    var identity = OidcLoginIdentityMapper.Map(ctx.Principal, resourceServer.Claims);
                    if (identity == null)
                    {
                        ctx.HandleResponse();
                        Logger(ctx.HttpContext).LogWarning("OpenID Connect sign-in without a subject or an e-mail");
                        ctx.Response.Redirect(OidcLoginDefaults.FailedPath);
                        return Task.CompletedTask;
                    }

                    //only what the sign-in needs, kept briefly in the external authentication cookie
                    ctx.Principal = ExternalRegistrationContext.CreatePrincipal(identity);
                    ctx.Properties!.IsPersistent = false;
                    ctx.Properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(OidcLoginDefaults.PendingLifetime);
                    return Task.CompletedTask;
                },
                OnRemoteFailure = ctx =>
                {
                    ctx.HandleResponse();
                    Logger(ctx.HttpContext).LogWarning(ctx.Failure, "OpenID Connect sign-in failed");
                    ctx.Response.Redirect(OidcLoginDefaults.FailedPath);
                    return Task.CompletedTask;
                }
            };
        });
    }

    private static ILogger Logger(Microsoft.AspNetCore.Http.HttpContext context) =>
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(OidcLoginAuthenticationBuilder));

    public int Priority => 503;
}
