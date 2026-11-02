using System.Reflection;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     Loads one planned plugin. A fault of the plugin never escapes: it becomes
///     <see cref="PluginInfo.LoadError" />, so one plugin folder cannot stop the application.
/// </summary>
public static class PluginLoader
{
    public const string NoPluginClassError = "has no IPlugin class";

    /// <summary>
    ///     Loads the candidate's assemblies into <paramref name="context" /> and finds its plugin class.
    /// </summary>
    /// <param name="prepareFile">The shadow-copy step; the file to load in place of the given one</param>
    /// <returns>
    ///     The plugin. When <see cref="PluginInfo.LoadError" /> is set, <see cref="PluginInfo.PluginType" /> and
    ///     <see cref="PluginInfo.ReferencedAssembly" /> are null.
    /// </returns>
    public static PluginInfo Load(PluginCandidate candidate, bool installed, AssemblyLoadContext context,
        Func<FileInfo, FileInfo> prepareFile)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(prepareFile);

        var plugin = Describe(candidate, installed);
        try
        {
            Assembly main = null;
            var assemblies = new List<Assembly>();
            foreach (var path in candidate.AssemblyPaths)
            {
                var file = prepareFile(new FileInfo(path));
                var assembly = LoadFromPath(context, file.FullName);
                assemblies.Add(assembly);
                if (!string.Equals(path, candidate.MainAssemblyPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                main = assembly;
                plugin.PluginFileName = file.Name;
            }

            //every assembly is loaded before any type is resolved, so the order of the files does not matter
            Type pluginType = null;
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                if (assembly == main)
                    pluginType = types.FirstOrDefault(IsPluginClass);
            }

            if (pluginType == null)
                return Failed(plugin, NoPluginClassError);

            plugin.ReferencedAssembly = main;
            plugin.PluginType = pluginType;
            return plugin;
        }
        catch (ReflectionTypeLoadException ex)
        {
            return Failed(plugin, ex.LoaderExceptions.FirstOrDefault(x => x != null)?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            //FileLoadException, FileNotFoundException, BadImageFormatException and whatever else a plugin's
            //files can cause; the plugin is reported, the application goes on
            return Failed(plugin, ex.Message);
        }
    }

    /// <summary>
    ///     The plugin as its manifest describes it, without loading anything.
    /// </summary>
    internal static PluginInfo Describe(PluginCandidate candidate, bool installed)
    {
        var manifest = candidate.Manifest;
        return new PluginInfo {
            FriendlyName = manifest.FriendlyName,
            Group = manifest.Group,
            SystemName = manifest.SystemName,
            Version = manifest.Version,
            SupportedVersion = manifest.SupportedVersion,
            Author = manifest.Author,
            PluginFileName = Path.GetFileName(candidate.MainAssemblyPath),
            OriginalAssemblyFile = new FileInfo(candidate.MainAssemblyPath),
            AssemblyNames = new HashSet<string>(candidate.AssemblyNames, StringComparer.OrdinalIgnoreCase),
            Installed = installed
        };
    }

    /// <summary>
    ///     A file already loaded into the context (it resolved as another plugin's dependency) is reused;
    ///     loading it again would fail with "Assembly with same name is already loaded".
    /// </summary>
    private static Assembly LoadFromPath(AssemblyLoadContext context, string path)
    {
        var loaded = context.Assemblies.FirstOrDefault(x =>
            string.Equals(x.Location, path, StringComparison.OrdinalIgnoreCase));
        return loaded ?? context.LoadFromAssemblyPath(path);
    }

    private static bool IsPluginClass(Type type)
    {
        return typeof(IPlugin).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract;
    }

    private static PluginInfo Failed(PluginInfo plugin, string error)
    {
        plugin.LoadError = error;
        plugin.PluginType = null;
        plugin.ReferencedAssembly = null;
        return plugin;
    }
}
