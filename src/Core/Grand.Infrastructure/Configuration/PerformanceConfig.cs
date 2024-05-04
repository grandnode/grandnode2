namespace Grand.Infrastructure.Configuration;

/// <summary>
///     Represents a Performance Config
/// </summary>
public class PerformanceConfig
{
    /// <summary>
    ///     Gets or sets a value indicating whether we compress response
    /// </summary>
    public bool UseResponseCompression { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether ignore InstallUrlMiddleware
    /// </summary>
    public bool IgnoreInstallUrlMiddleware { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether ignore DbVersionCheckMiddleware
    /// </summary>
    public bool IgnoreDbVersionCheckMiddleware { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether ignore IgnoreUsePoweredByMiddleware
    /// </summary>
    public bool IgnoreUsePoweredByMiddleware { get; set; }
}