using System.Reflection;

namespace Grand.Infrastructure.TypeSearch;

/// <summary>
///     Classes implementing this interface provide information about types
/// </summary>
public interface ITypeSearcher
{
    IList<Assembly> GetAssemblies();
    IEnumerable<Type> ClassesOfType<T>();
    IEnumerable<Type> ClassesOfType(Type assignTypeFrom);
    IEnumerable<Type> ClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies);
}