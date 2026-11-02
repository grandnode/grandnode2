using Grand.Infrastructure.Configuration;
using Grand.SharedKernel;
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

    #endregion

    #region Methods

    /// <summary>
    ///     Returns a collection of all referenced plugin assemblies that have been shadow copied
    /// </summary>
    public static IEnumerable<PluginInfo> ReferencedPlugins { get; set; }


    /// <summary>
    ///     Load plugins
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Load(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration, IWebHostEnvironment hostEnvironment)
    {
        _config = new ExtensionsConfig();
        configuration.GetSection("Extensions").Bind(_config);

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

            var referencedPlugins = new List<PluginInfo>();
            foreach (var problem in scan.Problems)
            {
                _logger.LogError("Plugin folder {PluginFolder} could not be loaded: {LoadError}",
                    problem.Folder, problem.Message);
                referencedPlugins.Add(new PluginInfo {
                    SystemName = problem.Folder,
                    FriendlyName = problem.Folder,
                    LoadError = problem.Message
                });
            }

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
            ResolveDependenciesFrom(compatible, prepareFile);

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
                var plugin = PluginLoader.Load(candidate, installed, AssemblyLoadContext.Default, prepareFile);
                referencedPlugins.Add(plugin);

                //controllers and views only for an installed plugin - an uninstalled one must not be routable
                if (plugin.LoadError == null && plugin.Installed)
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

                //register the plugin class - also when not installed, Install resolves it from DI
                if (plugin.PluginType != null)
                    mvcCoreBuilder.Services.AddScoped(plugin.PluginType);
            }

            ReferencedPlugins = referencedPlugins;

            var failed = referencedPlugins.FirstOrDefault(x => x.Installed && x.LoadError != null);
            if (failed != null && _config.FailOnPluginLoadError)
                throw new GrandException(
                    $"The installed plugin '{failed.SystemName}' could not be loaded: {failed.LoadError}");
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
    private static void ResolveDependenciesFrom(IEnumerable<PluginCandidate> plugins,
        Func<FileInfo, FileInfo> prepareFile)
    {
        var paths = plugins
            .SelectMany(x => x.AssemblyPaths)
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        if (_resolveDependency != null)
            AssemblyLoadContext.Default.Resolving -= _resolveDependency;

        _resolveDependency = (context, name) =>
            name.Name != null && paths.TryGetValue(name.Name, out var path)
                ? context.LoadFromAssemblyPath(prepareFile(new FileInfo(path)).FullName)
                : null;
        AssemblyLoadContext.Default.Resolving += _resolveDependency;
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