namespace Grand.Business.Core.Interfaces.ExportImport;

public interface IImportManager<T>
{
    Task Import(Stream stream, CancellationToken cancellationToken = default);
}