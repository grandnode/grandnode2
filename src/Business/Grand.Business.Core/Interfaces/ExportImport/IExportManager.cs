namespace Grand.Business.Core.Interfaces.ExportImport;

public interface IExportManager<T>
{
    /// <summary>
    ///     Export list of entity to XLSX
    /// </summary>
    /// <param name="entity">list of entity</param>
    /// <param name="cancellationToken">Stops loading the data the columns need; the workbook is not built</param>
    Task<byte[]> Export(IEnumerable<T> entity, CancellationToken cancellationToken = default);
}