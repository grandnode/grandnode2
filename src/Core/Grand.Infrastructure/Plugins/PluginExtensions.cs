using System.Reflection;
using System.Text.Json;

namespace Grand.Infrastructure.Plugins;


public sealed class PluginPaths
{
    private static PluginPaths _instance;
    private static readonly Lock _lock = new();
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
        ArgumentNullException.ThrowIfNull(type);

        return OnlyInstalledPlugins(type.Assembly);
    }

    /// <summary>
    ///     Whether the assembly may contribute types: true unless it belongs only to plugins that are not
    ///     active (not installed, or failed to load). Any assembly of a plugin counts, not only the one
    ///     carrying <see cref="PluginInfoAttribute" />; a dependency several plugins ship counts while one
    ///     of them is active.
    /// </summary>
    public static bool OnlyInstalledPlugins(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var name = assembly.GetName().Name;
        var owners = PluginManager.ReferencedPlugins?
            .Where(x => x.AssemblyNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return owners is not { Count: > 0 } || owners.Any(x => x.IsActive);
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
        ArgumentNullException.ThrowIfNullOrEmpty(systemName);

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
        ArgumentNullException.ThrowIfNullOrEmpty(systemName);

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
        if (alreadyMarkedAsInstalled)
            installedPluginSystemNames.Remove(systemName);

        await SaveInstalledPluginsFile(installedPluginSystemNames, filePath);
    }
}