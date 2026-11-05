using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Modules;

/// <summary>
///     Loads the modules enabled under FeatureManagement, each into a load context of its own, as plugins are
///     loaded: the module shares the host's assemblies and resolves its private libraries from its folder.
/// </summary>
public static class ModuleLoader
{
    private const string MODULE_SECTION_PATH = "FeatureManagement";
    private const string MODULES_DIRECTORY = "Modules";
    private const string LOGGER_NAME = "ModuleManager";

    //the contexts of modules that failed to load; their assemblies stay loaded, so scans must skip them
    private static readonly ConditionalWeakTable<AssemblyLoadContext, object> FailedContexts = new();

    private static List<(string FeatureName, bool IsEnabled)> GetFeatureList(IConfiguration configuration)
    {
        var featureSection = configuration.GetSection(MODULE_SECTION_PATH);
        if (!featureSection.Exists())
        {
            return new List<(string, bool)>();
        }

        return featureSection.GetChildren()
            .Select(feature => (
                FeatureName: feature.Key,
                IsEnabled: bool.TryParse(feature.Value, out var isEnabled) && isEnabled
            ))
            .ToList();
    }

    /// <summary>
    ///     Loads the enabled modules. A module that cannot be loaded is logged and left out, or, with
    ///     Extensions:FailOnPluginLoadError, stops the application.
    /// </summary>
    /// <exception cref="GrandException">A module failed to load and Extensions:FailOnPluginLoadError is set</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LoadModules(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        LoadModules(mvcCoreBuilder, configuration, hostingEnvironment,
            (name, mainAssemblyPath, hostAssemblyNames) =>
                new IsolatedLoadContext(name, mainAssemblyPath, hostAssemblyNames),
            () => GetLoggerFactory(mvcCoreBuilder.Services));
    }

    /// <param name="createContext">
    ///     Creates a module's own context from its name, main assembly path and the host assembly names; tests
    ///     create collectible ones
    /// </param>
    /// <param name="getLoggerFactory">The logger factory, asked for only when there are modules to load</param>
    internal static void LoadModules(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration,
        IWebHostEnvironment hostingEnvironment, Func<string, string, ISet<string>, AssemblyLoadContext> createContext,
        Func<ILoggerFactory> getLoggerFactory)
    {
        var modules = GetFeatureList(configuration);
        if (modules.Count == 0)
        {
            return;
        }

        var config = new ExtensionsConfig();
        configuration.GetSection("Extensions").Bind(config);
        var logger = getLoggerFactory().CreateLogger(LOGGER_NAME);
        var hostAssemblyNames = PluginManager.HostAssemblyNames();
        foreach (var module in modules)
        {
            if (!module.IsEnabled)
            {
                logger.LogInformation("Module '{ModuleName}' is disabled.", module.FeatureName);
                continue;
            }

            var error = LoadModule(module.FeatureName, hostingEnvironment, mvcCoreBuilder, logger,
                mainAssemblyPath => createContext(module.FeatureName, mainAssemblyPath, hostAssemblyNames));
            if (error == null)
                continue;

            logger.LogCritical("Module '{ModuleName}' could not be loaded: {LoadError}", module.FeatureName, error);
            if (config.FailOnPluginLoadError)
                throw new GrandException($"The module '{module.FeatureName}' could not be loaded: {error}");
        }
    }

    /// <summary>
    ///     Whether the assembly belongs to a module that failed to load; its types must not be scanned
    /// </summary>
    internal static bool IsFailedModule(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var context = AssemblyLoadContext.GetLoadContext(assembly);
        return context != null && FailedContexts.TryGetValue(context, out _);
    }

    /// <returns>The reason the module could not be loaded; null when it is loaded, or has no files to load</returns>
    private static string LoadModule(string moduleName, IWebHostEnvironment hostingEnvironment,
        IMvcCoreBuilder mvcCoreBuilder, ILogger logger, Func<string, AssemblyLoadContext> createContext)
    {
        var modulePath = GetModulePath(hostingEnvironment, moduleName);
        var moduleFile = Path.Combine(modulePath, $"{moduleName}.dll");

        if (!File.Exists(moduleFile))
        {
            logger.LogWarning("Module '{ModuleName}' does not exist at path: {ModulePath}",
                moduleName, moduleFile);
            return null;
        }

        AssemblyLoadContext context = null;
        try
        {
            context = createContext(moduleFile);
            var assembly = context.LoadFromAssemblyPath(moduleFile);

            //resolved now, so a dependency the context cannot provide fails the module here instead of a type scan
            //or a request
            _ = assembly.GetTypes();
            var error = ResolveReferences(assembly, context);
            if (error != null)
            {
                FailedContexts.AddOrUpdate(context, null);
                return error;
            }

            AddApplicationPart(mvcCoreBuilder, assembly);
            logger.LogInformation("Module '{ModuleName}' has been successfully loaded.", moduleName);
            return null;
        }
        catch (Exception ex)
        {
            if (context != null)
                FailedContexts.AddOrUpdate(context, null);

            return ex is ReflectionTypeLoadException typeLoad
                ? typeLoad.LoaderExceptions.FirstOrDefault(x => x != null)?.Message ?? ex.Message
                : ex.Message;
        }
    }

    /// <summary>
    ///     Resolves every assembly the module references. The application's copy of an assembly wins over the
    ///     module's, and the runtime binds it whatever its version, so one older than the module was built against
    ///     is reported here: it would fail only once a request reached the member it lacks.
    /// </summary>
    /// <returns>The reason the module cannot use its references; null when it can</returns>
    private static string ResolveReferences(Assembly assembly, AssemblyLoadContext context)
    {
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            var resolved = context.LoadFromAssemblyName(reference).GetName();
            if (reference.Version != null && resolved.Version != null && resolved.Version < reference.Version)
                return $"it is built against {reference.Name} {reference.Version}, " +
                       $"but the application provides {resolved.Version}";
        }

        return null;
    }

    /// <summary>
    ///     The application's logging, with every provider configured so far (console, event log, OpenTelemetry)
    /// </summary>
    private static ILoggerFactory GetLoggerFactory(IServiceCollection services)
        => services.BuildServiceProvider().GetService<ILoggerFactory>()
           ?? throw new InvalidOperationException("Logger factory not found in services.");

    private static string GetModulePath(IWebHostEnvironment hostingEnvironment, string moduleName)
           => Path.Combine(hostingEnvironment.ContentRootPath, MODULES_DIRECTORY, moduleName);

    /// <summary>
    ///     Adds the module's own assembly and its related assemblies; its dependencies are not application parts
    /// </summary>
    private static void AddApplicationPart(IMvcCoreBuilder mvcCoreBuilder, Assembly assembly)
    {
        //related parts are read first, so a failure adds nothing
        var relatedParts = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, false)
            .SelectMany(x => ApplicationPartFactory.GetApplicationPartFactory(x).GetApplicationParts(x))
            .ToList();

        mvcCoreBuilder.AddApplicationPart(assembly);
        foreach (var part in relatedParts)
            mvcCoreBuilder.PartManager.ApplicationParts.Add(part);
    }
}
