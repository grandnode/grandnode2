using Grand.Business.Common.Utilities;
using Grand.Business.Core.Interfaces.ExportImport;

namespace Grand.Business.Common.Services.ExportImport;

public class ExcelImportProvider : IImportDataProvider
{
    public async Task<IEnumerable<T>> Convert<T>(Stream stream)
    {
        var excel = new PowerExcelMapper {
            SkipBlankCells = false
        };
        return await excel.FetchAsync<T>(stream);
    }
}