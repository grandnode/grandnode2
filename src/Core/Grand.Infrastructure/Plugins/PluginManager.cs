using Grand.Data.LiteDb;
using Grand.Domain;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.TypeSearch;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     Plugin manager
/// </summary>
public static class PluginManager
{
    #region Const

    public const string CopyPath = "Plugins/bin";

    private static readonly Lock _synLock = new();
    
    #endregion

    #region Fields

    private static DirectoryInfo _copyFolder;
    private static DirectoryInfo _pluginFolder;
    private static ExtensionsConfig _config;
    private static ILogger _logger;

    //what LoadForInstall needs to load a plugin as Load would have
    private static Func<string, string, ISet<string>, AssemblyLoadContext> _createContext;
    private static ISet<string> _hostAssemblyNames;
    private static Func<PluginCandidate, DirectoryInfo> _prepareFolder;
    private static bool _useLiteDb;

    #endregion

    #region Methods

    /// <summary>
    ///     Returns a collection of all referenced plugin assemblies that have been shadow copied
    /// </summary>
    public static IEnumerable<PluginInfo> ReferencedPlugins { get; set; }

    /// <summary>
    ///     What <see cref="Load" /> saw on disk; null until plugins are loaded
    /// </summary>
    internal static PluginLoadSnapshot LoadSnapshot { get; set; }

    /// <summary>
    ///     Plugin changes made on disk since this process loaded its plugins; they take effect after a restart.
    ///     Reads the installed-plugins file and the plugin folders' metadata, never loads an assembly.
    /// </summary>
    public static IReadOnlyList<PluginPendingChange> GetPendingChanges()
    {
        return LoadSnapshot?.Compare() ?? [];
    }

    /// <summary>
    ///     Load plugins
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Load(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration, IWebHostEnvironment hostEnvironment)
    {
        Load(mvcCoreBuilder, configuration, hostEnvironment,
            (name, mainAssemblyPath, hostAssemblyNames) =>
                new IsolatedLoadContext(name, mainAssemblyPath, hostAssemblyNames));
    }

    /// <param name="createContext">
    ///     Creates a plugin's own context from its name, main assembly path and the host assembly names; tests
    ///     create collectible ones
    /// </param>
    internal static void Load(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration,
        IWebHostEnvironment hostEnvironment, Func<string, string, ISet<string>, AssemblyLoadContext> createContext)
    {
        _config = new ExtensionsConfig();
        configuration.GetSection("Extensions").Bind(_config);
        var dbConfig = new DatabaseConfig();
        configuration.GetSection("Database").Bind(dbConfig);

        lock (_synLock)
        {
            ArgumentNullException.ThrowIfNull(mvcCoreBuilder);

            _logger = mvcCoreBuilder.Services.BuildServiceProvider().GetService<ILoggerFactory>()
                .CreateLogger("PluginManager");

            _pluginFolder = new DirectoryInfo(Path.Combine(hostEnvironment.ContentRootPath, CommonPath.Plugins));
            _copyFolder = new DirectoryInfo(Path.Combine(hostEnvironment.ContentRootPath, CommonPath.Plugins, "bin"));

            var installedPluginSystemNames =
                (!string.IsNullOrEmpty(_config.InstalledPlugins)
                    ? _config.InstalledPlugins.Split(",").Select(x => x.Trim())
                    : PluginExtensions.ParseInstalledPluginsFile(PluginPaths.Instance.InstalledPluginsFile))
                .ToList();

            _logger.LogInformation("Creating shadow copy folder and querying for dlls");
            Directory.CreateDirectory(_pluginFolder.FullName);
            Directory.CreateDirectory(_copyFolder.FullName);
            if (_config.PluginShadowCopy)
                //clear out the files of the flat layout older versions shadow copied to; each plugin's own
                //folder below is kept, and its unchanged files are not copied again
                foreach (var f in _copyFolder.GetFiles("*", SearchOption.TopDirectoryOnly))
                {
                    _logger.LogInformation("Deleting {FName}", f.Name);
                    try
                    {
                        var fileName = Path.GetFileName(f.FullName);
                        if (fileName.Equals("index.htm", StringComparison.OrdinalIgnoreCase))
                            continue;

                        File.Delete(f.FullName);
                    }
                    catch (Exception exc)
                    {
                        _logger.LogError(exc, "PluginManager");
                    }
                }

            //plan from metadata first: nothing is loaded for a folder that cannot be loaded
            var hostAssemblyNames = HostAssemblyNames();
            var scan = PluginFolderScanner.Scan(_pluginFolder, _config.PluginSkipLoadingPattern, hostAssemblyNames);
            foreach (var warning in scan.Warnings)
                _logger.LogWarning("{PluginWarning}", warning);

            //before anything is loaded or shadow copied: the plugin folders as found
            LoadSnapshot = PluginLoadSnapshot.Capture(_pluginFolder, scan, installedPluginSystemNames,
                string.IsNullOrEmpty(_config.InstalledPlugins) ? PluginPaths.Instance.InstalledPluginsFile : null,
                _config.PluginSkipLoadingPattern);

            var referencedPlugins = new List<PluginInfo>();

            _createContext = createContext;
            _hostAssemblyNames = hostAssemblyNames;
            _prepareFolder = _config.PluginShadowCopy ? ShadowCopyFolder : candidate => candidate.Folder;
            _useLiteDb = dbConfig.UseLiteDb;

            foreach (var candidate in scan.Plugins)
            {
                if (candidate.Manifest.SupportedVersion != GrandVersion.SupportedPluginVersion)
                {
                    _logger.LogInformation("Incompatible plugin {PluginSystemName}", candidate.Manifest.SystemName);
                    //listed as not installed, and not loaded
                    referencedPlugins.Add(PluginLoader.Describe(candidate, false));
                    continue;
                }

                var installed = installedPluginSystemNames.Contains(candidate.Manifest.SystemName,
                    StringComparer.OrdinalIgnoreCase);
                if (!installed)
                {
                    //listed from its manifest; nothing of it is loaded until it is installed (LoadForInstall)
                    var listed = PluginLoader.Describe(candidate, false);
                    listed.Candidate = candidate;
                    referencedPlugins.Add(listed);
                    continue;
                }

                var plugin = PluginLoader.Load(candidate, true, ContextFactory(candidate), _prepareFolder);
                referencedPlugins.Add(plugin);

                //controllers and views only for an installed plugin - an uninstalled one must not be routable
                if (plugin.LoadError == null)
                {
                    var error = AddApplicationPart(mvcCoreBuilder, plugin);
                    if (error != null)
                    {
                        plugin.LoadError = error;
                        plugin.PluginType = null;
                        plugin.ReferencedAssembly = null;
                    }
                }

                if (plugin.LoadError != null)
                {
                    _logger.LogError("Plugin folder {PluginFolder} could not be loaded: {LoadError}",
                        candidate.Folder.Name, plugin.LoadError);
                    continue;
                }

                //register the plugin class; one loaded later to be installed is created without DI
                mvcCoreBuilder.Services.AddScoped(plugin.PluginType);
            }

            //after the loaded plugins: a lookup by a system name a second folder duplicates finds the working one.
            //The installed plugin of such a name is that folder's, so the second folder is not listed installed
            var listedSystemNames = referencedPlugins.Select(x => x.SystemName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var installedNotListed = installedPluginSystemNames.Where(x => !listedSystemNames.Contains(x)).ToList();
            foreach (var problem in scan.Problems)
            {
                _logger.LogError("Plugin folder {PluginFolder} could not be loaded: {LoadError}",
                    problem.Folder, problem.Message);
                referencedPlugins.AddRange(PluginLoader.DescribeProblem(problem, installedNotListed));
            }

            ReferencedPlugins = referencedPlugins;

            PluginLoader.ThrowOnLoadError(referencedPlugins, scan.Problems, _config.FailOnPluginLoadError);
        }
    }

    /// <summary>
    ///     Loads a plugin that was listed without being loaded (an uninstalled one), so it can be installed by the
    ///     running process: into a context of its own, as <see cref="Load" /> would, with its LiteDB mappers built.
    ///     Its startup classes, handlers and controllers are registered by the restart that follows the install, and
    ///     until then it stays not installed, so it contributes no types to any scan.
    ///     Does nothing for a plugin already loaded, failed or with nothing to load; safe from concurrent requests.
    ///     Never throws because of the plugin: its fault becomes <see cref="PluginInfo.LoadError" />.
    /// </summary>
    public static PluginInfo LoadForInstall(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        lock (_synLock)
        {
            var candidate = plugin.Candidate;
            if (plugin.ReferencedAssembly != null || plugin.LoadError != null || candidate == null ||
                _createContext == null)
                return plugin;

            _logger?.LogInformation("Loading plugin {PluginSystemName} to install it", plugin.SystemName);
            var loaded = PluginLoader.Load(candidate, plugin.Installed, ContextFactory(candidate), _prepareFolder);
            //warming here races with concurrent LiteDB reads: every entity mapper lives in one shared
            //BsonMapper.Global dictionary, read without a lock, so a request reading any type while these few
            //inserts run can fail. Accepted: the window is a few inserts during an admin-initiated install, and
            //the alternative is a restart before Install. On MongoDB none of this applies.
            if (loaded.LoadError == null && _useLiteDb)
                loaded.LoadError = WarmLiteDbMappers(loaded);

            plugin.PluginFileName = loaded.PluginFileName;
            plugin.AssemblyNames = loaded.AssemblyNames;
            plugin.LoadContext = loaded.LoadContext;
            plugin.LoadError = loaded.LoadError;
            plugin.Candidate = null;
            if (loaded.LoadError != null)
            {
                _logger?.LogError("Plugin {PluginSystemName} could not be loaded: {LoadError}", plugin.SystemName,
                    loaded.LoadError);
                return plugin;
            }

            plugin.PluginType = loaded.PluginType;
            //last: a request that sees the assembly skips the lock and reads the type
            plugin.ReferencedAssembly = loaded.ReferencedAssembly;
            return plugin;
        }
    }

    /// <summary>
    ///     Find a plugin by some type which is located into the same assembly plugin
    /// </summary>
    /// <param name="typeAssembly">Type</param>
    /// <returns>Plugin descriptor if exists; otherwise null</returns>
    public static PluginInfo FindPlugin(Type typeAssembly)
    {
        ArgumentNullException.ThrowIfNull(typeAssembly);

        return FindPlugin(typeAssembly.GetTypeInfo().Assembly);
    }

    /// <summary>
    ///     Find the plugin an assembly belongs to: the plugin whose load context holds it, or, for an assembly
    ///     outside every plugin's context, the plugin with that main assembly or shipping an assembly of that name
    /// </summary>
    /// <param name="assembly">Assembly</param>
    /// <returns>Plugin descriptor if exists; otherwise null</returns>
    public static PluginInfo FindPlugin(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var plugins = ReferencedPlugins?.ToList();
        if (plugins == null)
            return null;

        var name = assembly.GetName().Name;
        return OwnerOf(assembly, plugins)
               ?? plugins.FirstOrDefault(plugin => plugin.ReferencedAssembly != null
                                                   && plugin.ReferencedAssembly.FullName!.Equals(assembly.FullName,
                                                       StringComparison.OrdinalIgnoreCase))
               ?? plugins.FirstOrDefault(plugin =>
                   plugin.AssemblyNames.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     The plugin whose own load context holds the assembly; null for an assembly of the default context or
    ///     of a context no plugin owns
    /// </summary>
    internal static PluginInfo OwnerOf(Assembly assembly, IEnumerable<PluginInfo> plugins)
    {
        var context = AssemblyLoadContext.GetLoadContext(assembly);
        if (context == null || context == AssemblyLoadContext.Default || plugins == null)
            return null;

        return plugins.FirstOrDefault(plugin => ReferenceEquals(plugin.LoadContext, context));
    }


    /// <summary>
    ///     Clear plugins
    /// </summary>
    public static void ClearPlugins()
    {
        var filePath = PluginPaths.Instance.InstalledPluginsFile;
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    #endregion

    #region Utilities

    /// <summary>
    ///     Simple names of the assemblies the application itself provides (its trusted platform assemblies and
    ///     whatever the default context has loaded); a plugin's copy of one is never loaded
    /// </summary>
    internal static HashSet<string> HostAssemblyNames()
    {
        var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        var names = trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        names.UnionWith(AssemblyLoadContext.Default.Assemblies
            .Select(x => x.GetName().Name)
            .Where(x => !string.IsNullOrEmpty(x)));
        return names;
    }

    /// <summary>
    ///     Creates the candidate's own context once its folder is prepared and its main assembly path is known
    /// </summary>
    private static Func<string, AssemblyLoadContext> ContextFactory(PluginCandidate candidate)
    {
        var createContext = _createContext;
        var hostAssemblyNames = _hostAssemblyNames;
        return mainAssemblyPath => createContext(candidate.Manifest.SystemName, mainAssemblyPath, hostAssemblyNames);
    }

    /// <summary>
    ///     Builds the mappers of the plugin's entities before its install uses them. LiteDB mappers are not safe to
    ///     build while requests read them (see <see cref="LiteDBMapperWarmup" />), and requests are running here:
    ///     see the accepted race at the call site. Returns the error, if any.
    /// </summary>
    private static string WarmLiteDbMappers(PluginInfo plugin)
    {
        try
        {
            var assemblies = plugin.LoadContext.Assemblies
                .Where(x => plugin.AssemblyNames.Contains(x.GetName().Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
            LiteDBMapperWarmup.Warm(new TypeSearcher().ClassesOfType(typeof(BaseEntity), assemblies));
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    ///     Copies the plugin's folder, as a whole, to its own folder under Plugins/bin, so the plugin is loaded from
    ///     the copy and its own folder can be replaced while the application runs. A file is copied only when its
    ///     size or last write time differs from the copy's. Only this plugin's copy is touched.
    /// </summary>
    /// <returns>The folder to load the plugin from</returns>
    private static DirectoryInfo ShadowCopyFolder(PluginCandidate candidate)
    {
        var source = candidate.Folder;
        var target = new DirectoryInfo(Path.Combine(_copyFolder.FullName, source.Name));

        var copies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in source.GetFiles("*", SearchOption.AllDirectories))
        {
            var copy = new FileInfo(Path.Combine(target.FullName, Path.GetRelativePath(source.FullName, file.FullName)));
            copies.Add(copy.FullName);
            if (copy.Exists && copy.Length == file.Length && copy.LastWriteTimeUtc == file.LastWriteTimeUtc)
                continue;

            Directory.CreateDirectory(copy.DirectoryName!);
            CopyFile(file, copy);
        }

        //what the plugin no longer ships, so its context cannot resolve it
        foreach (var stale in target.GetFiles("*", SearchOption.AllDirectories).Where(x => !copies.Contains(x.FullName)))
            try
            {
                stale.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Stale shadow copy {FullName} could not be deleted", stale.FullName);
            }

        return target;
    }

    private static void CopyFile(FileInfo source, FileInfo copy)
    {
        try
        {
            File.Copy(source.FullName, copy.FullName, true);
        }
        catch (IOException) when (copy.Exists)
        {
            //a copy in use (another process still running from it, or a debugger) cannot be overwritten, but it
            //can be renamed, which frees its name; the renamed file is removed as stale by a later start
            _logger.LogInformation("{FullName} is locked, attempting to rename", copy.FullName);
            File.Move(copy.FullName, copy.FullName + "." + Guid.NewGuid().ToString("N") + ".old");
            File.Copy(source.FullName, copy.FullName, true);
        }

        //compared on the next start; not every file system keeps it on copy
        File.SetLastWriteTimeUtc(copy.FullName, source.LastWriteTimeUtc);
    }

    /// <summary>
    ///     Adds the plugin's controllers and views; returns the error when its parts cannot be read
    /// </summary>
    private static string AddApplicationPart(IMvcCoreBuilder mvcCoreBuilder, PluginInfo plugin)
    {
        try
        {
            //we can now register the plugin definition
            _logger.LogInformation("Adding to ApplicationParts: '{0}'", plugin.SystemName);

            //related parts are read first, so a failure adds nothing
            var relatedParts = RelatedAssemblyAttribute.GetRelatedAssemblies(plugin.ReferencedAssembly, false)
                .SelectMany(x => ApplicationPartFactory.GetApplicationPartFactory(x).GetApplicationParts(x))
                .ToList();

            mvcCoreBuilder.AddApplicationPart(plugin.ReferencedAssembly);
            foreach (var part in relatedParts)
                mvcCoreBuilder.PartManager.ApplicationParts.Add(part);

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    #endregion
}