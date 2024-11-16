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
                var types = assembly.GetTypes()
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
        return AssembliesInAppDomain();
    }

    #endregion
}