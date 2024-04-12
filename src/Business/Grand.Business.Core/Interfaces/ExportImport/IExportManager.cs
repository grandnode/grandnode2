namespace Grand.Business.Core.Interfaces.ExportImport;

public interface IExportManager<T>
{
    /// <summary>
    ///     Export list of entity to XLSX
    /// </summary>
    /// <param name="entity">list of entity</param>
    Task<byte[]> Export(IEnumerable<T> entity);
}