using Grand.SharedKernel;
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
    ///     Loads the candidate's assemblies into a load context of its own and finds its plugin class.
    /// </summary>
    /// <param name="createContext">
    ///     Creates the plugin's context for the main assembly at the given path (in the prepared folder)
    /// </param>
    /// <param name="prepareFolder">The shadow-copy step; the folder to load the plugin from in place of its own</param>
    /// <returns>
    ///     The plugin. When <see cref="PluginInfo.LoadError" /> is set, <see cref="PluginInfo.PluginType" /> and
    ///     <see cref="PluginInfo.ReferencedAssembly" /> are null.
    /// </returns>
    public static PluginInfo Load(PluginCandidate candidate, bool installed,
        Func<string, AssemblyLoadContext> createContext, Func<PluginCandidate, DirectoryInfo> prepareFolder)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(createContext);
        ArgumentNullException.ThrowIfNull(prepareFolder);

        var plugin = Describe(candidate, installed);
        try
        {
            var folder = prepareFolder(candidate);
            string Prepared(string path) =>
                Path.Combine(folder.FullName, Path.GetRelativePath(candidate.Folder.FullName, path));

            var mainPath = Prepared(candidate.MainAssemblyPath);
            var context = createContext(mainPath);
            plugin.LoadContext = context;

            var main = context.LoadFromAssemblyPath(mainPath);
            plugin.PluginFileName = Path.GetFileName(mainPath);

            //the others by name, so the context resolves each as it would for a reference (deps.json first)
            var assemblies = new List<Assembly> { main };
            foreach (var path in candidate.AssemblyPaths.Where(x =>
                         !string.Equals(x, candidate.MainAssemblyPath, StringComparison.OrdinalIgnoreCase)))
                assemblies.Add(context.LoadFromAssemblyName(AssemblyName.GetAssemblyName(Prepared(path))));

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

            //a reference used only inside a method body is not needed by GetTypes; resolved here, one the context
            //cannot provide (such as another plugin's assembly) fails the load instead of a later request
            foreach (var assembly in assemblies.Where(IsPluginCode))
            foreach (var reference in assembly.GetReferencedAssemblies())
                context.LoadFromAssemblyName(reference);

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
            //FileLoadException, FileNotFoundException, BadImageFormatException, an unresolvable deps.json and
            //whatever else a plugin's files can cause; the plugin is reported, the application goes on
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
    ///     The list entries for a folder the scan could not plan: one per system name it declares, installed
    ///     when that name is installed, or one named after the folder when its plugin cannot be identified.
    /// </summary>
    public static IEnumerable<PluginInfo> DescribeProblem(PluginLoadProblem problem,
        IEnumerable<string> installedSystemNames)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(installedSystemNames);

        if (problem.SystemNames.Count == 0)
            return [
                new PluginInfo {
                    SystemName = problem.Folder,
                    FriendlyName = problem.Folder,
                    LoadError = problem.Message
                }
            ];

        var installed = installedSystemNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return problem.SystemNames.Select(name => new PluginInfo {
            SystemName = name,
            FriendlyName = name,
            LoadError = problem.Message,
            Installed = installed.Contains(name)
        }).ToList();
    }

    /// <summary>
    ///     With <paramref name="failOnPluginLoadError" />, throws when an installed plugin failed to load, or
    ///     when a folder failed before its plugin could be identified - it may be an installed one.
    /// </summary>
    /// <exception cref="GrandException">The plugin or folder that failed, and why</exception>
    public static void ThrowOnLoadError(IEnumerable<PluginInfo> plugins, IEnumerable<PluginLoadProblem> problems,
        bool failOnPluginLoadError)
    {
        if (!failOnPluginLoadError)
            return;

        var failed = plugins.FirstOrDefault(x => x.Installed && x.LoadError != null);
        if (failed != null)
            throw new GrandException(
                $"The installed plugin '{failed.SystemName}' could not be loaded: {failed.LoadError}");

        var unidentified = problems.FirstOrDefault(x => x.SystemNames.Count == 0);
        if (unidentified != null)
            throw new GrandException(
                $"The plugin folder '{unidentified.Folder}' could not be loaded: {unidentified.Message}");
    }

    /// <summary>
    ///     The plugin's own code, built against GrandNode; a third-party library may reference optional
    ///     assemblies it never loads, so its references are left to the runtime
    /// </summary>
    private static bool IsPluginCode(Assembly assembly)
    {
        var core = typeof(IPlugin).Assembly.GetName().Name;
        return assembly.GetReferencedAssemblies().Any(x => string.Equals(x.Name, core, StringComparison.OrdinalIgnoreCase));
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
