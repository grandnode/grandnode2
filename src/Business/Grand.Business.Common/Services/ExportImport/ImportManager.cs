using Grand.Business.Core.Interfaces.ExportImport;

namespace Grand.Business.Common.Services.ExportImport;

public class ImportManager<T> : IImportManager<T> where T : class
{
    private readonly IImportDataObject<T> _importDataObject;
    private readonly IImportDataProvider _importDataProvider;

    public ImportManager(IImportDataProvider importDataProvider, IImportDataObject<T> importDataObject)
    {
        _importDataProvider = importDataProvider;
        _importDataObject = importDataObject;
    }

    public async Task Import(Stream stream)
    {
        var data = await _importDataProvider.Convert<T>(stream);
        await _importDataObject.Execute(data);
    }
}