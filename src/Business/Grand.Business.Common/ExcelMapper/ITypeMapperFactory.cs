using System;

namespace Ganss.Excel
{
    /// <summary>
    /// A caching factory of <see cref="TypeMapper"/> objects.
    /// </summary>
    public interface ITypeMapperFactory
    {
        /// <summary>
        /// Creates a <see cref="TypeMapper"/> for the specified type.
        /// </summary>
        /// <param name="type">The type to create a <see cref="TypeMapper"/> object for.</param>
        /// <returns>A <see cref="TypeMapper"/> for the specified type.</returns>
        TypeMapper Create(Type type);

        /// <summary>
        /// Creates a <see cref="TypeMapper"/> for the specified object.
        /// </summary>
        /// <param name="o">The object to create a <see cref="TypeMapper"/> object for.</param>
        /// <returns>A <see cref="TypeMapper"/> for the specified object.</returns>
        TypeMapper Create(object o);
    }
}