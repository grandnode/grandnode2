using Ganss.Excel;
using Grand.Business.Core.Interfaces.ExportImport;

namespace Grand.Business.Common.Services.ExportImport
{
    public class ExcelImportProvider : IImportDataProvider
    {
        public IEnumerable<T> Convert<T>(Stream stream)
        {
            return new ExcelMapper(stream).Fetch<T>();
        }
    }
}
