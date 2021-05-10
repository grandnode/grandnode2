using System;
using System.Collections.Generic;
using System.Reflection;

namespace Grand.Infrastructure.TypeSearchers
{
    /// <summary>
    /// Classes implementing this interface provide information about types 
    /// </summary>
    public interface ITypeSearcher
    {
        IList<Assembly> GetAssemblies();

        IEnumerable<Type> ClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true);

        IEnumerable<Type> ClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true);

        IEnumerable<Type> ClassesOfType<T>(bool onlyConcreteClasses = true);

    }
}
