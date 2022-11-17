namespace Grand.Business.Core.Interfaces.ExportImport
{
    public interface IImportDataProvider
    {
        IEnumerable<T> Convert<T>(Stream stream);
    }
}
