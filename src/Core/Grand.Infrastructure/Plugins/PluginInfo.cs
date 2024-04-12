using Grand.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Grand.Infrastructure.Plugins;

public sealed class PluginInfo
{
    public PluginInfo(
        FileInfo originalAssemblyFile,
        Assembly referencedAssembly,
        Type pluginType)
        : this()
    {
        ReferencedAssembly = referencedAssembly;
        OriginalAssemblyFile = originalAssemblyFile;
        PluginType = pluginType;
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
    ///     The assembly that has been shadow copied that is active in the application
    /// </summary>
    public Assembly ReferencedAssembly { get; internal set; }

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

    public T Instance<T>(IServiceProvider serviceProvider) where T : class, IPlugin
    {
        object instance;
        try
        {
            instance = serviceProvider.GetRequiredService(PluginType);
        }
        catch
        {
            throw new GrandException(
                $"Plugin has not been registered getRequiredService - dependency - {PluginType.FullName}");
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