using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Grand.Web.Common.Security.Oidc;

public static class OidcAuthenticationBuilderExtensions
{
    /// <summary>
    ///     The signing algorithms the Oidc scheme accepts; the scheme selector routes a token here by the same list
    /// </summary>
    public static IReadOnlyList<string> AsymmetricAlgorithms { get; } = [
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
        //the work context asks the selector too; it forwards the Bearer scheme only while the resource server is enabled
        builder.Services.TryAddSingleton<OidcSchemeSelector>();
        if (enabled)
            builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(
                services => services.GetRequiredService<OidcSchemeSelector>());

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
                //negative throws when the options are built (first request); the validator reports the value
                ClockSkew = TimeSpan.FromSeconds(Math.Max(0, config.ClockSkewSeconds)),
                NameClaimType = config.Claims.Subject
            };
            options.Events = new OidcJwtBearerEvents(config);
            if (enabled)
                configure?.Invoke(options);
        });

        //after JwtBearer's own post-configuration, which derives the metadata address and creates the backchannel
        if (enabled)
            builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(
                new PostConfigureOptions<JwtBearerOptions>(OidcResourceServerDefaults.AuthenticationScheme,
                    UseOidcConfigurationManager));

        return builder;
    }

    /// <summary>
    ///     Replaces the configuration manager JwtBearer built from Authority with one that reports retrieval failures
    /// </summary>
    private static void UseOidcConfigurationManager(JwtBearerOptions options)
    {
        if (options.Configuration != null ||
            options.ConfigurationManager?.GetType() != typeof(ConfigurationManager<OpenIdConnectConfiguration>))
            return;

        options.ConfigurationManager = new OidcConfigurationManager(options.MetadataAddress,
            new HttpDocumentRetriever(options.Backchannel) { RequireHttps = options.RequireHttpsMetadata }) {
            RefreshInterval = options.RefreshInterval,
            AutomaticRefreshInterval = options.AutomaticRefreshInterval
        };
    }
}
