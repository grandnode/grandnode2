using Grand.Business.Core.Utilities.ExportImport;

namespace Grand.Business.Core.Interfaces.ExportImport;

public interface ISchemaProperty<T>
{
    /// <summary>
    ///     Gets the export columns for the given rows. Data the columns need (pictures, countries)
    ///     is loaded here, so the column getters never block on a task.
    /// </summary>
    /// <param name="items">Rows that will be exported; pass the same instances to the export provider</param>
    Task<PropertyByName<T>[]> GetProperties(IEnumerable<T> items);
}
