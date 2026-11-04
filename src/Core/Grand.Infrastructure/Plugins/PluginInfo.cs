using Grand.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Plugins;

public sealed class PluginInfo
{
    private Assembly _referencedAssembly;

    public PluginInfo(
        FileInfo originalAssemblyFile,
        Assembly referencedAssembly,
        Type pluginType)
        : this()
    {
        ReferencedAssembly = referencedAssembly;
        OriginalAssemblyFile = originalAssemblyFile;
        PluginType = pluginType;
        if (referencedAssembly != null)
            AssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { referencedAssembly.GetName().Name! };
    }

    public PluginInfo()
    {
    }

    /// <summary>
    ///     Plugin type
    /// </summary>
    public string PluginFileName { get; set; }

    /// <summary>
    ///     Plugin type
    /// </summary>
    public Type PluginType { get; set; }

    /// <summary>
    ///     The plugin's main assembly once it is loaded: at startup for an installed plugin, on demand
    ///     (<see cref="PluginManager.LoadForInstall" />) for one being installed; null until then
    /// </summary>
    /// <remarks>
    ///     Set last when a plugin loads on demand, so a request that sees it also sees <see cref="PluginType" />
    /// </remarks>
    public Assembly ReferencedAssembly
    {
        get => Volatile.Read(ref _referencedAssembly);
        internal set => Volatile.Write(ref _referencedAssembly, value);
    }

    /// <summary>
    ///     The scanned folder <see cref="PluginManager.LoadForInstall" /> loads; null when there is nothing to load
    ///     on demand (the plugin loaded at startup, is built for another version, or was not found by a scan)
    /// </summary>
    internal PluginCandidate Candidate { get; set; }

    /// <summary>
    ///     The plugin's own load context, once loading it began; it holds the plugin's assemblies, also those of
    ///     a plugin that failed to load. Null for a plugin that was not loaded.
    /// </summary>
    internal AssemblyLoadContext LoadContext { get; set; }

    /// <summary>
    ///     The original assembly file that a shadow copy was made from it
    /// </summary>
    public FileInfo OriginalAssemblyFile { get; internal set; }

    /// <summary>
    ///     Gets or sets the plugin group
    /// </summary>
    public string Group { get; set; }

    /// <summary>
    ///     Gets or sets the friendly name
    /// </summary>
    public string FriendlyName { get; set; }

    /// <summary>
    ///     Gets or sets the system name
    /// </summary>
    public string SystemName { get; set; }

    /// <summary>
    ///     Gets or sets the version
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    ///     Gets or sets the supported version
    /// </summary>
    public string SupportedVersion { get; set; }

    /// <summary>
    ///     Gets or sets the author
    /// </summary>
    public string Author { get; set; }

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets the value indicating whether plugin is installed
    /// </summary>
    public bool Installed { get; set; }

    /// <summary>
    ///     Why the plugin could not be loaded; null when it loaded
    /// </summary>
    public string LoadError { get; internal set; }

    /// <summary>
    ///     Simple names of the plugin's assemblies, compared case-insensitively. Taken from the plugin
    ///     folder, so they are known whether or not the plugin was loaded.
    /// </summary>
    public IReadOnlyCollection<string> AssemblyNames { get; internal set; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Installed and loaded: only an active plugin contributes types to the application
    /// </summary>
    public bool IsActive => Installed && LoadError == null;

    /// <summary>
    ///     Creates the plugin, loading it first when it was listed without being loaded (an uninstalled plugin).
    ///     An installed plugin is resolved from DI; one loaded on demand is not registered there until the restart,
    ///     so it is created with its constructor dependencies taken from <paramref name="serviceProvider" />.
    /// </summary>
    /// <exception cref="GrandException">The plugin cannot be loaded or created</exception>
    public T Instance<T>(IServiceProvider serviceProvider) where T : class, IPlugin
    {
        if (ReferencedAssembly == null && LoadError == null)
            PluginManager.LoadForInstall(this);

        if (PluginType == null)
            throw new GrandException(LoadError != null
                ? $"Plugin {SystemName} could not be loaded: {LoadError}"
                : $"Plugin {SystemName} has no IPlugin class");

        object instance;
        try
        {
            instance = serviceProvider.GetService(PluginType)
                       ?? ActivatorUtilities.CreateInstance(serviceProvider, PluginType);
        }
        catch (Exception e)
        {
            throw new GrandException(
                $"Plugin {SystemName} ({PluginType.FullName}) could not be created: {e.Message}", e);
        }

        var typedInstance = instance as T;
        if (typedInstance != null)
            typedInstance.PluginInfo = this;
        return typedInstance;
    }

    public IPlugin Instance(IServiceProvider serviceProvider)
    {
        return Instance<IPlugin>(serviceProvider);
    }
}