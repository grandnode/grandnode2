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
using System.Collections.Concurrent;
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
    private static Func<AssemblyLoadContext, AssemblyName, Assembly> _resolveDependency;
    private static AssemblyLoadContext _resolveDependencyContext;

    //what LoadForInstall needs to load a plugin as Load would have
    private static AssemblyLoadContext _loadContext;
    private static Func<FileInfo, FileInfo> _prepareFile;
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
        Load(mvcCoreBuilder, configuration, hostEnvironment, AssemblyLoadContext.Default);
    }

    /// <param name="loadContext">The context plugin assemblies are loaded into; tests pass a collectible one</param>
    internal static void Load(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration,
        IWebHostEnvironment hostEnvironment, AssemblyLoadContext loadContext)
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
            var binFiles = _copyFolder.GetFiles("*", SearchOption.AllDirectories);
            if (_config.PluginShadowCopy)
                //clear out shadow plugins
                foreach (var f in binFiles)
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
            var scan = PluginFolderScanner.Scan(_pluginFolder, _config.PluginSkipLoadingPattern, HostAssemblyNames());
            foreach (var warning in scan.Warnings)
                _logger.LogWarning("{PluginWarning}", warning);

            //before anything is loaded or shadow copied: the plugin folders as found
            LoadSnapshot = PluginLoadSnapshot.Capture(_pluginFolder, scan, installedPluginSystemNames,
                string.IsNullOrEmpty(_config.InstalledPlugins) ? PluginPaths.Instance.InstalledPluginsFile : null,
                _config.PluginSkipLoadingPattern);

            var referencedPlugins = new List<PluginInfo>();

            //a dependency can be asked for twice (resolved for one plugin, then loaded by the one shipping it);
            //it is copied once
            var shadowCopies = new ConcurrentDictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
            Func<FileInfo, FileInfo> prepareFile = _config.PluginShadowCopy
                ? file => shadowCopies.GetOrAdd(file.FullName,
                    _ => ShadowCopyFile(file, Directory.CreateDirectory(_copyFolder.FullName)))
                : file => file;

            var compatible = scan.Plugins
                .Where(x => x.Manifest.SupportedVersion == GrandVersion.SupportedPluginVersion)
                .ToList();
            ResolveDependenciesFrom(compatible, loadContext, prepareFile);
            _loadContext = loadContext;
            _prepareFile = prepareFile;
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

                if (!_config.PluginShadowCopy)
                    DeleteDepsFiles(candidate.Folder);

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

                var plugin = PluginLoader.Load(candidate, true, loadContext, prepareFile);
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

            //after the loaded plugins: a lookup by a system name a second folder duplicates finds the working one
            foreach (var problem in scan.Problems)
            {
                _logger.LogError("Plugin folder {PluginFolder} could not be loaded: {LoadError}",
                    problem.Folder, problem.Message);
                referencedPlugins.AddRange(PluginLoader.DescribeProblem(problem, installedPluginSystemNames));
            }

            ReferencedPlugins = referencedPlugins;

            PluginLoader.ThrowOnLoadError(referencedPlugins, scan.Problems, _config.FailOnPluginLoadError);
        }
    }

    /// <summary>
    ///     Loads a plugin that was listed without being loaded (an uninstalled one), so it can be installed by the
    ///     running process: into the context <see cref="Load" /> uses, with its LiteDB mappers built. Its startup
    ///     classes, handlers and controllers are registered by the restart that follows the install, and until then
    ///     it stays not installed, so it contributes no types to any scan.
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
                _loadContext == null)
                return plugin;

            _logger?.LogInformation("Loading plugin {PluginSystemName} to install it", plugin.SystemName);
            var loaded = PluginLoader.Load(candidate, plugin.Installed, _loadContext, _prepareFile);
            //warming here races with concurrent LiteDB reads: every entity mapper lives in one shared
            //BsonMapper.Global dictionary, read without a lock, so a request reading any type while these few
            //inserts run can fail. Accepted: the window is a few inserts during an admin-initiated install, and
            //the alternative is a restart before Install. On MongoDB none of this applies.
            if (loaded.LoadError == null && _useLiteDb)
                loaded.LoadError = WarmLiteDbMappers(loaded);

            plugin.PluginFileName = loaded.PluginFileName;
            plugin.AssemblyNames = loaded.AssemblyNames;
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
    ///     Find the plugin an assembly belongs to
    /// </summary>
    /// <param name="assembly">Assembly</param>
    /// <returns>Plugin descriptor if exists; otherwise null</returns>
    public static PluginInfo FindPlugin(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return ReferencedPlugins?.FirstOrDefault(plugin => plugin.ReferencedAssembly != null
                                                           && plugin.ReferencedAssembly.FullName!.Equals(
                                                               assembly.FullName,
                                                               StringComparison.OrdinalIgnoreCase));
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
    ///     Simple names of the assemblies the application itself provides; a plugin's copy of one is never loaded
    /// </summary>
    private static HashSet<string> HostAssemblyNames()
    {
        var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        return trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     When two plugins ship the same dependency only one copy is loaded (see <see cref="PluginFolderScanner" />).
    ///     The plugin whose copy was dropped finds the kept one here, also before the plugin shipping it is loaded.
    /// </summary>
    private static void ResolveDependenciesFrom(IEnumerable<PluginCandidate> plugins, AssemblyLoadContext loadContext,
        Func<FileInfo, FileInfo> prepareFile)
    {
        var paths = plugins
            .SelectMany(x => x.AssemblyPaths)
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        if (_resolveDependency != null)
            _resolveDependencyContext.Resolving -= _resolveDependency;

        _resolveDependency = (context, name) =>
            name.Name != null && paths.TryGetValue(name.Name, out var path)
                ? context.LoadFromAssemblyPath(prepareFile(new FileInfo(path)).FullName)
                : null;
        _resolveDependencyContext = loadContext;
        loadContext.Resolving += _resolveDependency;
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
            var assemblies = _loadContext.Assemblies
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

    private static void DeleteDepsFiles(DirectoryInfo folder)
    {
        //remove deps.json files
        try
        {
            foreach (var f in folder.GetFiles("*.deps.json", SearchOption.TopDirectoryOnly))
                try
                {
                    File.Delete(f.FullName);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, "PluginManager");
                }
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "PluginManager");
        }
    }

    /// <summary>
    ///     Used to initialize plugins when running in Medium Trust
    /// </summary>
    /// <param name="plug"></param>
    /// <param name="shadowCopyPlugFolder"></param>
    /// <returns></returns>
    private static FileInfo ShadowCopyFile(FileInfo plug, DirectoryInfo shadowCopyPlugFolder)
    {
        var shouldCopy = true;
        var shadowCopiedPlug = new FileInfo(Path.Combine(shadowCopyPlugFolder.FullName, plug.Name));

        //check if a shadow copied file already exists and if it does, check if it's updated, if not don't copy
        if (shadowCopiedPlug.Exists)
        {
            //it's better to use LastWriteTimeUTC, but not all file systems have this property
            //maybe it is better to compare file hash?
            var areFilesIdentical = shadowCopiedPlug.CreationTimeUtc.Ticks >= plug.CreationTimeUtc.Ticks;
            if (areFilesIdentical)
            {
                _logger.LogInformation("Not copying; files appear identical: {Name}", shadowCopiedPlug.Name);
                return shadowCopiedPlug;
            }

            //delete an existing file
            _logger.LogInformation("New plugin found; Deleting the old file: {Name}", shadowCopiedPlug.Name);
            try
            {
                File.Delete(shadowCopiedPlug.FullName);
            }
            catch (Exception ex)
            {
                shouldCopy = false;
                _logger.LogError(ex, "PluginManager");
            }
        }

        if (!shouldCopy) return shadowCopiedPlug;
        try
        {
            File.Copy(plug.FullName, shadowCopiedPlug.FullName, true);
        }
        catch (IOException)
        {
            _logger.LogInformation("{FullName} is locked, attempting to rename", shadowCopiedPlug.FullName);
            //this occurs when the files are locked,
            //for some reason devenv locks plugin files some times and for another crazy reason you are allowed to rename them
            //which releases the lock, so that it what we are doing here, once it's renamed, we can re-shadow copy
            try
            {
                var oldFile = shadowCopiedPlug.FullName + Guid.NewGuid().ToString("N") + ".old";
                File.Move(shadowCopiedPlug.FullName, oldFile);
            }
            catch (IOException exc)
            {
                throw new IOException(shadowCopiedPlug.FullName + " rename failed, cannot initialize plugin", exc);
            }

            //ok, we've made it this far, now retry the shadow copy
            File.Copy(plug.FullName, shadowCopiedPlug.FullName, true);
        }

        return shadowCopiedPlug;
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