using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Grand.Business.Common.Services.ExportImport
{
    public class ExcelExportProvider : IExportProvider
    {
        public virtual byte[] ExportToByte<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport)
        {
            using (var stream = new MemoryStream())
            {
                IWorkbook xlPackage = new XSSFWorkbook();
                ISheet worksheet = xlPackage.CreateSheet(typeof(T).Name);
                var manager = new PropertyManager<T>(properties);

                WriteCaption(worksheet, properties);

                var row = 1;

                foreach (var items in itemsToExport)
                {
                    manager.CurrentObject = items;
                    WriteToXlsx(manager, properties, worksheet, row++);
                }
                xlPackage.Write(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Write caption (first row) to XLSX worksheet
        /// </summary>
        /// <param name="ISheet">sheet</param>
        private void WriteCaption<T>(ISheet sheet, PropertyByName<T>[] properties)
        {
            IRow row = sheet.CreateRow(0);
            foreach (var caption in properties)
            {
                row.CreateCell(caption.PropertyOrderPosition).SetCellValue(caption.PropertyName);
            }
        }

        /// <summary>
        /// Write object data to XLSX worksheet
        /// </summary>
        /// <param name="worksheet">worksheet</param>
        /// <param name="row">Row index</param>
        private void WriteToXlsx<T>(PropertyManager<T> manager, PropertyByName<T>[] properties, ISheet sheet, int row)
        {
            if (manager.CurrentObject == null)
                return;

            IRow _row = sheet.CreateRow(row);
            foreach (var prop in properties)
            {
                var cellValue = (prop.GetProperty(manager.CurrentObject)?.ToString());
                if (cellValue != null && cellValue.Length >= 32767) // 32767 is the max char size of an excel cell
                {
                    cellValue = cellValue.Substring(0, 32767); //Truncate the content to max size.
                }
                _row.CreateCell(prop.PropertyOrderPosition).SetCellValue(cellValue);
            }
        }
    }
}
