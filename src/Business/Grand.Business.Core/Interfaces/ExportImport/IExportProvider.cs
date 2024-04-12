using Grand.Business.Core.Utilities.ExportImport;

namespace Grand.Business.Core.Interfaces.ExportImport;

public interface IExportProvider
{
    byte[] ExportToByte<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport);
    IExportProvider BuilderExportToByte<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport);
    byte[] BuilderExportToByte();
}