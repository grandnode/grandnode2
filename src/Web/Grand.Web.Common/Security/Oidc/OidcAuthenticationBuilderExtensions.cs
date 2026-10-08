using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Grand.Web.Common.Security.Oidc;

public static class OidcAuthenticationBuilderExtensions
{
    private static readonly string[] AsymmetricAlgorithms = [
        SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha512,
        SecurityAlgorithms.RsaSsaPssSha256, SecurityAlgorithms.RsaSsaPssSha384, SecurityAlgorithms.RsaSsaPssSha512,
        SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512
    ];

    /// <summary>
    ///     Registers the Oidc JwtBearer scheme. When the resource server is disabled the scheme still exists, so an
    ///     endpoint naming it answers 401 instead of failing, but it has no issuer and no keys: every token fails.
    /// </summary>
    /// <param name="builder">Authentication builder</param>
    /// <param name="config">Resource server configuration</param>
    /// <param name="configure">Applied last when enabled (tests inject a static OpenID configuration)</param>
    public static AuthenticationBuilder AddOidcResourceServer(this AuthenticationBuilder builder,
        OidcResourceServerConfig config, Action<JwtBearerOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        var enabled = config.Enabled;

        builder.AddJwtBearer(OidcResourceServerDefaults.AuthenticationScheme, options =>
        {
            if (enabled)
            {
                options.Authority = config.Authority;
                options.RequireHttpsMetadata = config.RequireHttpsMetadata;
            }
            else
            {
                options.Configuration = new OpenIdConnectConfiguration();
            }

            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                //the audience depends on the matched resource and the store URL - checked in TokenValidated
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                ValidAlgorithms = AsymmetricAlgorithms,
                ClockSkew = TimeSpan.FromSeconds(config.ClockSkewSeconds),
                NameClaimType = config.Claims.Subject
            };
            options.Events = new OidcJwtBearerEvents(config);
            if (enabled)
                configure?.Invoke(options);
        });

        return builder;
    }
}
