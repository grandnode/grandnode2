using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Api.Infrastructure;

/// <summary>
///     Fails fast at startup when an enabled API is configured with a missing, placeholder or too-short JWT signing
///     key. A weak/known key lets anyone forge valid tokens, so outside Development this aborts startup instead of
///     silently running with a forgeable secret.
/// </summary>
public class ApiSecurityStartup : IStartupApplication
{
    //shipped placeholder in appsettings.json - must never be used to sign real tokens
    private const string PlaceholderSecret = "your private secret key to use api";

    //HS256 needs a >= 256-bit key; the signing key is the raw ASCII bytes of the secret, so require >= 32 chars
    private const int MinSecretLength = 32;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        var logger = application.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ApiSecurityStartup));

        var backend = new BackendAPIConfig();
        application.Configuration.GetSection("BackendAPI").Bind(backend);
        Validate("BackendAPI", backend.Enabled, backend.SecretKey, webHostEnvironment, logger);

        var frontend = new FrontendAPIConfig();
        application.Configuration.GetSection("FrontendAPI").Bind(frontend);
        Validate("FrontendAPI", frontend.Enabled, frontend.SecretKey, webHostEnvironment, logger);
    }

    private static void Validate(string section, bool enabled, string secret, IWebHostEnvironment env, ILogger logger)
    {
        if (!enabled)
            return;

        if (IsStrong(secret))
            return;

        var message =
            $"{section}.SecretKey is missing, still set to the placeholder, or shorter than {MinSecretLength} " +
            "characters. Configure a strong, unique secret (e.g. via the environment variable " +
            $"{section}__SecretKey or a secret store) - a weak key allows forging valid API tokens.";

        //in Development we only warn so local setups keep working; in every other environment we abort startup
        if (env.IsDevelopment())
            logger.LogWarning("{Message}", message);
        else
            throw new InvalidOperationException(message);
    }

    private static bool IsStrong(string secret)
    {
        return !string.IsNullOrWhiteSpace(secret)
               && secret.Length >= MinSecretLength
               && !string.Equals(secret, PlaceholderSecret, StringComparison.Ordinal);
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public int Priority => 0;
    public bool BeforeConfigure => true;
}
