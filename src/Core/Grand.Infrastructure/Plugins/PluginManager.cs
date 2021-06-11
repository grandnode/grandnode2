using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace Grand.Infrastructure.Plugins
{
    /// <summary>
    /// Plugin manager
    /// </summary>
    public static class PluginManager
    {
        #region Const

        public const string CopyPath = "Plugins/bin";

        private static object _synLock = new object();

        #endregion

        #region Fields

        private static DirectoryInfo _copyFolder;
        private static DirectoryInfo _pluginFolder;
        private static AppConfig _config;

        #endregion

        #region Methods

        /// <summary>
        /// Returns a collection of all referenced plugin assemblies that have been shadow copied
        /// </summary>
        public static IEnumerable<PluginInfo> ReferencedPlugins { get; set; }


        /// <summary>
        /// Load plugins
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Load(IMvcCoreBuilder mvcCoreBuilder, AppConfig config)
        {
            lock (_synLock)
            {
                if (mvcCoreBuilder == null)
                    throw new ArgumentNullException(nameof(mvcCoreBuilder));

                _config = config ?? throw new ArgumentNullException(nameof(config));

                _pluginFolder = new DirectoryInfo(CommonPath.PluginsPath);
                _copyFolder = new DirectoryInfo(CommonPath.PluginsCopyPath);

                var referencedPlugins = new List<PluginInfo>();
                try
                {
                    var installedPluginSystemNames = PluginExtensions.ParseInstalledPluginsFile(CommonPath.InstalledPluginsFilePath);

                    Log.Information("Creating shadow copy folder and querying for dlls");
                    Directory.CreateDirectory(_pluginFolder.FullName);
                    Directory.CreateDirectory(_copyFolder.FullName);
                    var binFiles = _copyFolder.GetFiles("*", SearchOption.AllDirectories);
                    if (config.ClearPluginShadowDirectoryOnStartup)
                    {
                        //clear out shadow plugins
                        foreach (var f in binFiles)
                        {
                            Log.Information($"Deleting {f.Name}");
                            try
                            {
                                var fileName = Path.GetFileName(f.FullName);
                                if (fileName.Equals("index.htm", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                File.Delete(f.FullName);
                            }
                            catch (Exception exc)
                            {
                                Log.Error(exc, "PluginManager");
                            }
                        }
                    }

                    //load description files
                    foreach (var plugin in GetPluginInfo())
                    {
                        if (plugin.SupportedVersion != GrandVersion.SupportedPluginVersion)
                        {
                            Log.Information($"Incompatible plugin {plugin.SystemName}");
                            //set as not installed
                            referencedPlugins.Add(plugin);
                            continue;
                        }

                        //some validation
                        if (string.IsNullOrWhiteSpace(plugin.SystemName))
                            throw new Exception(string.Format("The plugin '{0}' has no system name.", plugin.SystemName));
                        if (referencedPlugins.Contains(plugin))
                            throw new Exception(string.Format("The plugin with '{0}' system name is already defined", plugin.SystemName));

                        //set 'Installed' property
                        plugin.Installed = installedPluginSystemNames
                            .FirstOrDefault(x => x.Equals(plugin.SystemName, StringComparison.OrdinalIgnoreCase)) != null;

                        try
                        {
                            if (!config.PluginShadowCopy)
                            {
                                //remove deps.json files 
                                var depsFiles = plugin.OriginalAssemblyFile.Directory.GetFiles("*.deps.json", SearchOption.TopDirectoryOnly);
                                foreach (var f in depsFiles)
                                {
                                    try
                                    {
                                        File.Delete(f.FullName);
                                    }
                                    catch (Exception exc)
                                    {
                                        Log.Error(exc, "PluginManager");
                                    }
                                }
                            }

                            //main plugin file
                            AddApplicationPart(mvcCoreBuilder, plugin.ReferencedAssembly, plugin.SystemName, plugin.PluginFileName);

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
                            var msg = string.Format("Plugin '{0}'. ", plugin.FriendlyName);
                            foreach (var e in ex.LoaderExceptions)
                                msg += e.Message + Environment.NewLine;

                            var fail = new Exception(msg, ex);
                            throw fail;
                        }
                        catch (Exception ex)
                        {
                            var msg = string.Format("Plugin '{0}'. {1}", plugin.FriendlyName, ex.Message);

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
        /// Find a plugin by some type which is located into the same assembly plugin
        /// </summary>
        /// <param name="typeAssembly">Type</param>
        /// <returns>Plugin descriptor if exists; otherwise null</returns>
        public static PluginInfo FindPlugin(Type typeAssembly)
        {
            if (typeAssembly == null)
                throw new ArgumentNullException(nameof(typeAssembly));

            if (ReferencedPlugins == null)
                return null;

            return ReferencedPlugins.FirstOrDefault(plugin => plugin.ReferencedAssembly != null
                && plugin.ReferencedAssembly.FullName.Equals(typeAssembly.GetTypeInfo().Assembly.FullName, StringComparison.OrdinalIgnoreCase));
        }


        /// <summary>
        /// Clear plugins
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
                throw new ArgumentNullException("pluginFolder");

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
            var _plug = _config.PluginShadowCopy ? ShadowCopyFile(pluginFile, Directory.CreateDirectory(_copyFolder.FullName)) : pluginFile;

            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(_plug.FullName);

            var pluginInfo = assembly.GetCustomAttribute<PluginInfoAttribute>();
            if (pluginInfo == null)
            {
                return null;
            }

            var plugin = new PluginInfo
            {
                FriendlyName = pluginInfo.FriendlyName,
                Group = pluginInfo.Group,
                SystemName = pluginInfo.SystemName,
                Version = pluginInfo.Version,
                SupportedVersion = pluginInfo.SupportedVersion,
                Author = pluginInfo.Author,
                PluginFileName = _plug.Name,
                OriginalAssemblyFile = pluginFile,
                ReferencedAssembly = assembly
            };

            return plugin;
        }

        /// <summary>
        /// Used to initialize plugins when running in Medium Trust
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
                    Log.Information($"Not copying; files appear identical: {shadowCopiedPlug.Name}");
                    return shadowCopiedPlug;
                }
                else
                {
                    //delete an existing file
                    Log.Information($"New plugin found; Deleting the old file: {shadowCopiedPlug.Name}");
                    try
                    {
                        File.Delete(shadowCopiedPlug.FullName);
                    }
                    catch (Exception ex)
                    {
                        shouldCopy = false;
                        Log.Error(ex, "PluginManager");
                    }
                }
            }
            if (shouldCopy)
            {
                try
                {
                    File.Copy(plug.FullName, shadowCopiedPlug.FullName, true);
                }
                catch (IOException)
                {
                    Log.Information($"{shadowCopiedPlug.FullName} is locked, attempting to rename");
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
                Log.Information("Adding to ApplicationParts: '{0}'", systemName);
                mvcCoreBuilder.AddApplicationPart(assembly);

                var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false);
                foreach (var relatedAssembly in relatedAssemblies)
                {
                    var applicationPartFactory = ApplicationPartFactory.GetApplicationPartFactory(relatedAssembly);
                    foreach (var part in applicationPartFactory.GetApplicationParts(relatedAssembly))
                    {
                        mvcCoreBuilder.PartManager.ApplicationParts.Add(part);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PluginManager");
                throw new InvalidOperationException($"The plugin directory for the {systemName} file exists in a folder outside of the allowed grandnode folder hierarchy - exception because of {filename} - exception: {ex.Message}");
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
                Log.Error(ex, "RegisterPluginInterface");
            }
        }

        /// <summary>
        /// Determines if the folder is a bin plugin folder for a package
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private static bool IsPackagePluginFolder(DirectoryInfo folder)
        {
            if (folder == null) return false;
            if (folder.Name.Equals("bin", StringComparison.InvariantCultureIgnoreCase)) return false;
            if (folder.Parent == null) return false;
            if (!folder.Parent.Name.Equals("Plugins", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }


        #endregion
    }
}
