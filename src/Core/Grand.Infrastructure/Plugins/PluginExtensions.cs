using System.Text.Json;

namespace Grand.Infrastructure.Plugins;


public sealed class PluginPaths
{
    private static PluginPaths _instance;
    private static readonly object _lock = new();
    public static PluginPaths Instance => _instance ?? throw new InvalidOperationException("PluginPaths has not been initialized. Call Initialize first.");
    private readonly string _pluginPath;

    public string InstalledPluginsFile => _pluginPath;

    public static void Initialize(string settingsPath)
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                _instance ??= new PluginPaths(settingsPath);
            }
        }
    }

    private PluginPaths(string pluginPath)
    {
        _pluginPath = pluginPath;
    }
}

public static class PluginExtensions
{
    public static bool OnlyInstalledPlugins(Type type)
    {
        var plugin = PluginManager.FindPlugin(type);
        return plugin == null || plugin.Installed;
    }

    public static IList<string> ParseInstalledPluginsFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<string>();

        var text = File.ReadAllText(filePath);
        return string.IsNullOrEmpty(text) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(text);
    }
    private static async Task SaveInstalledPluginsFile(IList<string> pluginSystemNames, string filePath)
    {
        //serialize
        var result = JsonSerializer.Serialize(pluginSystemNames, new JsonSerializerOptions { WriteIndented = true });
        //save
        await File.WriteAllTextAsync(filePath, result);
        await Task.CompletedTask;
    }

    /// <summary>
    ///     Mark plugin as installed
    /// </summary>
    /// <param name="systemName">Plugin system name</param>
    public static async Task MarkPluginAsInstalled(string systemName)
    {
        if (string.IsNullOrEmpty(systemName))
            throw new ArgumentNullException(nameof(systemName));

        var filePath = PluginPaths.Instance.InstalledPluginsFile;
        if (!File.Exists(filePath))
            await using (File.Create(filePath))
            {
                //we use 'using' to close the file after it's created
            }

        var installedPluginSystemNames = ParseInstalledPluginsFile(filePath);

        var alreadyMarkedAsInstalled =
            installedPluginSystemNames.FirstOrDefault(x => x.Equals(systemName, StringComparison.OrdinalIgnoreCase)) !=
            null;
        if (!alreadyMarkedAsInstalled)
            installedPluginSystemNames.Add(systemName);

        await SaveInstalledPluginsFile(installedPluginSystemNames, filePath);
    }

    /// <summary>
    ///     Mark plugin as uninstalled
    /// </summary>
    /// <param name="systemName">Plugin system name</param>
    public static async Task MarkPluginAsUninstalled(string systemName)
    {
        if (string.IsNullOrEmpty(systemName))
            throw new ArgumentNullException(nameof(systemName));

        var filePath = PluginPaths.Instance.InstalledPluginsFile;
        if (!File.Exists(filePath))
            await using (File.Create(filePath))
            {
            }

        var installedPluginSystemNames = ParseInstalledPluginsFile(filePath);
        var alreadyMarkedAsInstalled =
            installedPluginSystemNames.FirstOrDefault(x => x.Equals(systemName, StringComparison.OrdinalIgnoreCase)) !=
            null;
        if (alreadyMarkedAsInstalled)
            installedPluginSystemNames.Remove(systemName);

        await SaveInstalledPluginsFile(installedPluginSystemNames, filePath);
    }
}