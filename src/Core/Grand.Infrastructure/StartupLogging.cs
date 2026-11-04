using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Grand.Infrastructure;

/// <summary>
///     Logging for code that runs while services are being registered, before the application's logger exists:
///     the console, filtered by the application's Logging section. Building a service provider for a logger at
///     that stage would create a second container and the singletons registered so far.
/// </summary>
internal static class StartupLogging
{
    public static ILoggerFactory CreateLoggerFactory(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return LoggerFactory.Create(builder => builder
            .AddConfiguration(configuration.GetSection("Logging"))
            .AddConsole());
    }
}
