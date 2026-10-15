namespace Grand.Business.Core.Interfaces.ExportImport;

public interface IImportDataObject<T>
{
    /// <summary>
    ///     Imports the rows of a spreadsheet. A cancelled token stops the import before the next row; rows already
    ///     saved stay saved.
    /// </summary>
    Task Execute(IEnumerable<T> data, CancellationToken cancellationToken = default);
}