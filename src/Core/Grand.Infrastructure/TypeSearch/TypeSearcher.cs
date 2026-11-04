using Grand.Infrastructure.Modules;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Roslyn;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Grand.Infrastructure.TypeSearch;

/// <summary>
///     A class that finds types needed by looping assemblies in the
///     currently executing AppDomain.
/// </summary>
public class TypeSearcher : ITypeSearcher
{
    #region Utilities

    //Shared by every instance, because TypeSearcher is created in many places during startup.
    //Loaded assemblies are never unloaded, so their types are read once.
    private static readonly ConcurrentDictionary<Assembly, Type[]> TypesByAssembly = new();

    private sealed record AssemblySnapshot(
        int AppDomainAssemblyCount,
        IEnumerable<PluginInfo> ReferencedPlugins,
        IEnumerable<ResultCompiler> ReferencedScripts,
        IList<Assembly> Assemblies);

    //Replaced as a whole, never mutated. A race only costs one redundant recomputation.
    private static volatile AssemblySnapshot _snapshot;

    private static IList<Assembly> CachedAssemblies()
    {
        var count = AppDomain.CurrentDomain.GetAssemblies().Length;
        var plugins = PluginManager.ReferencedPlugins;
        var scripts = RoslynCompiler.ReferencedScripts;

        var snapshot = _snapshot;
        if (snapshot != null && snapshot.AppDomainAssemblyCount == count &&
            ReferenceEquals(snapshot.ReferencedPlugins, plugins) &&
            ReferenceEquals(snapshot.ReferencedScripts, scripts))
            return snapshot.Assemblies;

        var assemblies = new ReadOnlyCollection<Assembly>(AssembliesInAppDomain());
        _snapshot = new AssemblySnapshot(count, plugins, scripts, assemblies);
        return assemblies;
    }

    private static IList<Assembly> AssembliesInAppDomain()
    {
        //one assembly per load context: plugins shipping the same library each load their own copy, and each
        //plugin's code uses the types of its own
        var addedAssemblies = new HashSet<(AssemblyLoadContext, string)>();
        var assemblies = new List<Assembly>();
        var currentAssem = Assembly.GetExecutingAssembly();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var product = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            var referencedAssemblies = assembly.GetReferencedAssemblies().ToList();
            if (referencedAssemblies.All(x => x.FullName != currentAssem.FullName) &&
                product?.Product != "grandnode") continue;
            var key = (AssemblyLoadContext.GetLoadContext(assembly), assembly.FullName);
            if (addedAssemblies.Contains(key)) continue;
            //an uninstalled plugin contributes no types to any scan
            if (!PluginExtensions.OnlyInstalledPlugins(assembly)) continue;
            //nor does a module that failed to load; its types may not even resolve
            if (ModuleLoader.IsFailedModule(assembly)) continue;
            assemblies.Add(assembly);
            addedAssemblies.Add(key);
        }

        //add scripts
        if (RoslynCompiler.ReferencedScripts == null) return assemblies;
        foreach (var scripts in RoslynCompiler.ReferencedScripts)
        {
            if (string.IsNullOrEmpty(scripts.ReferencedAssembly.FullName)) continue;
            var key = (AssemblyLoadContext.GetLoadContext(scripts.ReferencedAssembly), scripts.ReferencedAssembly.FullName);
            if (addedAssemblies.Contains(key)) continue;
            assemblies.Add(scripts.ReferencedAssembly);
            addedAssemblies.Add(key);
        }

        return assemblies;
    }

    #endregion

    #region Methods

    public IEnumerable<Type> ClassesOfType<T>()
    {
        return ClassesOfType(typeof(T));
    }

    public IEnumerable<Type> ClassesOfType(Type assignTypeFrom)
    {
        return ClassesOfType(assignTypeFrom, GetAssemblies());
    }

    public IEnumerable<Type> ClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies)
    {
        var result = new List<Type>();
        try
        {
            foreach (var assembly in assemblies)
            {
                var types = TypesByAssembly.GetOrAdd(assembly, static a => a.GetTypes())
                    .Where(type =>
                        (assignTypeFrom.IsAssignableFrom(type) ||
                         (assignTypeFrom.IsGenericTypeDefinition && DoesTypeImplementOpenGeneric(type, assignTypeFrom))) &&
                        !type.IsInterface &&
                        type.IsClass &&
                        !type.IsAbstract);

                result.AddRange(types);
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            var errorMessage = ex.LoaderExceptions
                .Aggregate(string.Empty, (current, e) => current + e!.Message + Environment.NewLine);

            var exception = new Exception(errorMessage, ex);
            Debug.WriteLine(exception.Message, exception);
            throw exception;
        }

        return result;
    }

    /// <summary>
    ///     Does type implement generic?
    /// </summary>
    /// <param name="type"></param>
    /// <param name="openGeneric"></param>
    /// <returns></returns>
    private static bool DoesTypeImplementOpenGeneric(Type type, Type openGeneric)
    {
        try
        {
            var genericTypeDefinition = openGeneric.GetGenericTypeDefinition();
            return (from implementedInterface in type.FindInterfaces((_, _) => true, null)
                    where implementedInterface.IsGenericType
                    select genericTypeDefinition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition()))
                .FirstOrDefault();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gets the assemblies related to the current implementation.</summary>
    /// <returns>A list of assemblies that should be loaded by the Grand factory.</returns>
    public virtual IList<Assembly> GetAssemblies()
    {
        return CachedAssemblies();
    }

    #endregion
}