using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     Plugin manager
/// </summary>
public static class PluginManager
{
    #region Const

    public const string CopyPath = "Plugins/bin";

    private static readonly object _synLock = new();

    #endregion

    #region Fields

    private static DirectoryInfo _copyFolder;
    private static DirectoryInfo _pluginFolder;
    private static ExtensionsConfig _config;
    private static ILogger _logger;

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
    public static void Load(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration)
    {
        _config = new ExtensionsConfig();
        configuration.GetSection("Extensions").Bind(_config);

        lock (_synLock)
        {
            if (mvcCoreBuilder == null)
                throw new ArgumentNullException(nameof(mvcCoreBuilder));

            _logger = mvcCoreBuilder.Services.BuildServiceProvider().GetService<ILoggerFactory>()
                .CreateLogger("PluginManager");

            _pluginFolder = new DirectoryInfo(CommonPath.PluginsPath);
            _copyFolder = new DirectoryInfo(CommonPath.PluginsCopyPath);

            var referencedPlugins = new List<PluginInfo>();
            try
            {
                var installedPluginSystemNames =
                    !string.IsNullOrEmpty(_config.InstalledPlugins)
                        ? _config.InstalledPlugins.Split(",").Select(x => x.Trim())
                        : PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);

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

                //load description files
                foreach (var plugin in GetPluginInfo())
                {
                    if (plugin.SupportedVersion != GrandVersion.SupportedPluginVersion)
                    {
                        _logger.LogInformation("Incompatible plugin {PluginSystemName}", plugin.SystemName);
                        //set as not installed
                        referencedPlugins.Add(plugin);
                        continue;
                    }

                    //some validation
                    if (string.IsNullOrWhiteSpace(plugin.SystemName))
                        throw new Exception($"The plugin '{plugin.SystemName}' has no system name.");
                    if (referencedPlugins.Contains(plugin))
                        throw new Exception($"The plugin with '{plugin.SystemName}' system name is already defined");

                    //set 'Installed' property
                    plugin.Installed = installedPluginSystemNames
                        .FirstOrDefault(x => x.Equals(plugin.SystemName, StringComparison.OrdinalIgnoreCase)) != null;

                    try
                    {
                        if (!_config.PluginShadowCopy)
                        {
                            //remove deps.json files 
                            var depsFiles =
                                plugin.OriginalAssemblyFile.Directory!.GetFiles("*.deps.json",
                                    SearchOption.TopDirectoryOnly);
                            foreach (var f in depsFiles)
                                try
                                {
                                    File.Delete(f.FullName);
                                }
                                catch (Exception exc)
                                {
                                    _logger.LogError(exc, "PluginManager");
                                }
                        }

                        //main plugin file
                        AddApplicationPart(mvcCoreBuilder, plugin.ReferencedAssembly, plugin.SystemName,
                            plugin.PluginFileName);

                        //register interface for IPlugin 
                        RegisterPluginInterface(mvcCoreBuilder, plugin.ReferencedAssembly);

                        //init plugin type
                        foreach (var t in plugin.ReferencedAssembly.GetTypes())
                            if (typeof(IPlugin).IsAssignableFrom(t))
                                if (!t.GetTypeInfo().IsInterface)
                                    if (t.GetTypeInfo().IsClass && !t.GetTypeInfo().IsAbstract)
                                    {
                                        plugin.PluginType = t;
                                        break;
                                    }

                        referencedPlugins.Add(plugin);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        var msg = $"Plugin '{plugin.FriendlyName}'. ";
                        msg = ex.LoaderExceptions.Aggregate(msg,
                            (current, e) => current + e!.Message + Environment.NewLine);

                        var fail = new Exception(msg, ex);
                        throw fail;
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Plugin '{plugin.FriendlyName}'. {ex.Message}";

                        var fail = new Exception(msg, ex);
                        throw fail;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = string.Empty;
                for (var e = ex; e != null; e = e.InnerException)
                    msg += e.Message + Environment.NewLine;

                var fail = new Exception(msg, ex);
                throw fail;
            }

            ReferencedPlugins = referencedPlugins;
        }
    }

    /// <summary>
    ///     Find a plugin by some type which is located into the same assembly plugin
    /// </summary>
    /// <param name="typeAssembly">Type</param>
    /// <returns>Plugin descriptor if exists; otherwise null</returns>
    public static PluginInfo FindPlugin(Type typeAssembly)
    {
        if (typeAssembly == null)
            throw new ArgumentNullException(nameof(typeAssembly));

        return ReferencedPlugins?.FirstOrDefault(plugin => plugin.ReferencedAssembly != null
                                                           && plugin.ReferencedAssembly.FullName!.Equals(
                                                               typeAssembly.GetTypeInfo().Assembly.FullName,
                                                               StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    ///     Clear plugins
    /// </summary>
    public static void ClearPlugins()
    {
        var filePath = CommonPath.InstalledPluginsFilePath;
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    #endregion

    #region Utilities

    private static IList<PluginInfo> GetPluginInfo()
    {
        if (_pluginFolder == null)
            throw new ArgumentNullException(nameof(_pluginFolder));

        var result = new List<PluginInfo>();
        foreach (var pluginFile in _pluginFolder.GetFiles("*.dll", SearchOption.AllDirectories))
        {
            if (!IsPackagePluginFolder(pluginFile.Directory))
                continue;

            if (!string.IsNullOrEmpty(_config.PluginSkipLoadingPattern)
                && Matches(pluginFile.Name, _config.PluginSkipLoadingPattern))
                continue;

            //prepare plugin info
            var plugins = PreparePluginInfo(pluginFile);
            if (plugins == null)
                continue;

            result.Add(plugins);
        }

        return result;
    }

    private static PluginInfo PreparePluginInfo(FileInfo pluginFile)
    {
        var plug = _config.PluginShadowCopy
            ? ShadowCopyFile(pluginFile, Directory.CreateDirectory(_copyFolder.FullName))
            : pluginFile;

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(plug.FullName);

        var pluginInfo = assembly.GetCustomAttribute<PluginInfoAttribute>();
        if (pluginInfo == null) return null;

        var plugin = new PluginInfo {
            FriendlyName = pluginInfo.FriendlyName,
            Group = pluginInfo.Group,
            SystemName = pluginInfo.SystemName,
            Version = pluginInfo.Version,
            SupportedVersion = pluginInfo.SupportedVersion,
            Author = pluginInfo.Author,
            PluginFileName = plug.Name,
            OriginalAssemblyFile = pluginFile,
            ReferencedAssembly = assembly
        };

        return plugin;
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

    private static bool Matches(string fullName, string pattern)
    {
        return Regex.IsMatch(fullName, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static void AddApplicationPart(IMvcCoreBuilder mvcCoreBuilder,
        Assembly assembly, string systemName, string filename)
    {
        try
        {
            //we can now register the plugin definition
            _logger.LogInformation("Adding to ApplicationParts: '{0}'", systemName);
            mvcCoreBuilder.AddApplicationPart(assembly);

            var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, false);
            foreach (var relatedAssembly in relatedAssemblies)
            {
                var applicationPartFactory = ApplicationPartFactory.GetApplicationPartFactory(relatedAssembly);
                foreach (var part in applicationPartFactory.GetApplicationParts(relatedAssembly))
                    mvcCoreBuilder.PartManager.ApplicationParts.Add(part);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PluginManager");
            throw new InvalidOperationException(
                $"The plugin directory for the {systemName} file exists in a folder outside of the allowed grandnode folder hierarchy - exception because of {filename} - exception: {ex.Message}");
        }
    }

    private static void RegisterPluginInterface(IMvcCoreBuilder mvcCoreBuilder, Assembly assembly)
    {
        try
        {
            foreach (var t in assembly.GetTypes())
                if (typeof(IPlugin).IsAssignableFrom(t))
                    mvcCoreBuilder.Services.AddScoped(t);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterPluginInterface");
        }
    }

    /// <summary>
    ///     Determines if the folder is a bin plugin folder for a package
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    private static bool IsPackagePluginFolder(DirectoryInfo folder)
    {
        if (folder == null) return false;
        if (folder.Name.Equals("bin", StringComparison.InvariantCultureIgnoreCase)) return false;
        return folder.Parent != null && folder.Parent.Name.Equals("Plugins", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}