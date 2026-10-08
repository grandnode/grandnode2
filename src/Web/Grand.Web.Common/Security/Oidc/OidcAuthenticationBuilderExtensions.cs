using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
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
    ///     Registers the Oidc JwtBearer scheme; nothing when the resource server is disabled
    /// </summary>
    /// <param name="builder">Authentication builder</param>
    /// <param name="config">Resource server configuration</param>
    /// <param name="configure">Applied last (tests inject a static OpenID configuration)</param>
    public static AuthenticationBuilder AddOidcResourceServer(this AuthenticationBuilder builder,
        OidcResourceServerConfig config, Action<JwtBearerOptions> configure = null)
    {
        if (config is not { Enabled: true })
            return builder;

        builder.AddJwtBearer(OidcResourceServerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = config.Authority;
            options.RequireHttpsMetadata = config.RequireHttpsMetadata;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                //the audience depends on the matched resource and the request host - checked in TokenValidated
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                ValidAlgorithms = AsymmetricAlgorithms,
                ClockSkew = TimeSpan.FromSeconds(config.ClockSkewSeconds),
                NameClaimType = config.Claims.Subject
            };
            options.Events = new OidcJwtBearerEvents(config);
            configure?.Invoke(options);
        });

        return builder;
    }
}
