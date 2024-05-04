namespace Grand.Business.Core.Interfaces.ExportImport;

public interface IImportDataProvider
{
    Task<IEnumerable<T>> Convert<T>(Stream stream);
}