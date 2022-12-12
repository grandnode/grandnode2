using Ganss.Excel;
using Grand.Business.Core.Interfaces.ExportImport;

namespace Grand.Business.Common.Services.ExportImport
{
    public class ExcelImportProvider : IImportDataProvider
    {
        public async Task<IEnumerable<T>> Convert<T>(Stream stream)
        {
            var excel = new ExcelMapper {
                SkipBlankCells = false
            };
            return await excel.FetchAsync<T>(stream);
        }
    }
}
