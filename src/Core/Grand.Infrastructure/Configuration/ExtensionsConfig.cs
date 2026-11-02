namespace Grand.Infrastructure.Configuration;

/// <summary>
///     Represents a Extensions Config
/// </summary>
public class ExtensionsConfig
{
    /// <summary>
    ///     Indicates whether we disabled upload plugins/themes
    /// </summary>
    public bool DisableUploadExtensions { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether copy dll plugin files to /Plugins/bin on application startup
    /// </summary>
    public bool PluginShadowCopy { get; set; }

    /// <summary>
    ///     A list of plugins to be ignored during start application - pattern
    /// </summary>
    public string PluginSkipLoadingPattern { get; set; }

    /// <summary>
    ///     Enable scripting C# applications to execute code.
    /// </summary>
    public bool UseRoslynScripts { get; set; }

    /// <summary>
    ///     List of installed plugins (comma separated)
    /// </summary>
    public string InstalledPlugins { get; set; }

    /// <summary>
    ///     Stop the application when an installed plugin fails to load. Off: the plugin is logged,
    ///     listed with its error and left inactive, and the application starts without it.
    /// </summary>
    public bool FailOnPluginLoadError { get; set; }
}