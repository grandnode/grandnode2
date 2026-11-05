using Grand.Data;
using Grand.SharedKernel;
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
    ///     carrying <see cref="PluginInfoAttribute" />. An assembly in a plugin's own load context belongs to that
    ///     plugin alone; one outside every plugin's context, matched by name, counts while any plugin shipping
    ///     that name is active.
    /// </summary>
    public static bool OnlyInstalledPlugins(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var plugins = PluginManager.ReferencedPlugins?.ToList();
        var owner = PluginManager.OwnerOf(assembly, plugins);
        if (owner != null)
            return owner.IsActive;

        var name = assembly.GetName().Name;
        var owners = plugins?
            .Where(x => x.AssemblyNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return owners is not { Count: > 0 } || owners.Any(x => x.IsActive);
    }

    /// <summary>
    ///     Reads the installed-plugins file older versions kept the list in; read only to take the list over into
    ///     the database. A file that is not valid throws instead of yielding an empty list, because an empty list
    ///     would silently deactivate every plugin.
    /// </summary>
    public static IList<string> ParseInstalledPluginsFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<string>();

        var text = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(text) ?? new List<string>();
        }
        catch (JsonException e)
        {
            throw new GrandException($"{filePath} is not a valid installed-plugins file: {e.Message}", e);
        }
    }

    /// <summary>
    ///     Mark plugin as installed
    /// </summary>
    /// <param name="systemName">Plugin system name</param>
    public static Task MarkPluginAsInstalled(string systemName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(systemName);

        return StateStore().SetInstalled(systemName, true);
    }

    /// <summary>
    ///     Mark plugin as uninstalled
    /// </summary>
    /// <param name="systemName">Plugin system name</param>
    public static Task MarkPluginAsUninstalled(string systemName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(systemName);

        return StateStore().SetInstalled(systemName, false);
    }

    private static IPluginStateStore StateStore()
    {
        return PluginManager.StateStore ??
               throw new InvalidOperationException("PluginManager.StateStore has not been set.");
    }
}
