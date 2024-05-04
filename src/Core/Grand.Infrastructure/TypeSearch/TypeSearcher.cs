using Grand.Infrastructure.Roslyn;
using System.Diagnostics;
using System.Reflection;

namespace Grand.Infrastructure.TypeSearch;

/// <summary>
///     A class that finds types needed by looping assemblies in the
///     currently executing AppDomain.
/// </summary>
public class TypeSearcher : ITypeSearcher
{
    #region Utilities

    private static IList<Assembly> AssembliesInAppDomain()
    {
        var addedAssemblyNames = new List<string>();
        var assemblies = new List<Assembly>();
        var currentAssem = Assembly.GetExecutingAssembly();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var product = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            var referencedAssemblies = assembly.GetReferencedAssemblies().ToList();
            if (referencedAssemblies.All(x => x.FullName != currentAssem.FullName) &&
                product?.Product != "grandnode") continue;
            if (addedAssemblyNames.Contains(assembly.FullName)) continue;
            assemblies.Add(assembly);
            addedAssemblyNames.Add(assembly.FullName);
        }

        //add scripts
        if (RoslynCompiler.ReferencedScripts == null) return assemblies;
        foreach (var scripts in RoslynCompiler.ReferencedScripts)
        {
            if (string.IsNullOrEmpty(scripts.ReferencedAssembly.FullName)) continue;
            if (addedAssemblyNames.Contains(scripts.ReferencedAssembly.FullName)) continue;
            assemblies.Add(scripts.ReferencedAssembly);
            addedAssemblyNames.Add(scripts.ReferencedAssembly.FullName);
        }

        return assemblies;
    }

    #endregion

    #region Methods

    public IEnumerable<Type> ClassesOfType<T>(bool onlyConcreteClasses = true)
    {
        return ClassesOfType(typeof(T), onlyConcreteClasses);
    }

    public IEnumerable<Type> ClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true)
    {
        return ClassesOfType(assignTypeFrom, GetAssemblies(), onlyConcreteClasses);
    }

    public IEnumerable<Type> ClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies,
        bool onlyConcreteClasses = true)
    {
        var result = new List<Type>();
        try
        {
            foreach (var a in assemblies)
            {
                var types = a.GetTypes();
                foreach (var t in types)
                {
                    if (!assignTypeFrom.IsAssignableFrom(t) && (!assignTypeFrom.IsGenericTypeDefinition ||
                                                                !DoesTypeImplementOpenGeneric(t, assignTypeFrom)))
                        continue;

                    if (t.IsInterface)
                        continue;

                    if (onlyConcreteClasses)
                    {
                        if (t.IsClass && !t.IsAbstract) result.Add(t);
                    }
                    else
                    {
                        result.Add(t);
                    }
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            var msg = ex.LoaderExceptions.Aggregate(string.Empty,
                (current, e) => current + e!.Message + Environment.NewLine);

            var fail = new Exception(msg, ex);
            Debug.WriteLine(fail.Message, fail);

            throw fail;
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
        return AssembliesInAppDomain();
    }

    #endregion
}