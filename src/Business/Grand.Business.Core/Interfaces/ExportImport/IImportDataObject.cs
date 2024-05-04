namespace Grand.Business.Core.Interfaces.ExportImport;

public interface IImportDataObject<T>
{
    Task Execute(IEnumerable<T> data);
}