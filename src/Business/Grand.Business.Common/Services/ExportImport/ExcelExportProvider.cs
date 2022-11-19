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
                    WriteToXlsx(properties, worksheet, items, row++);
                }
                xlPackage.Write(stream, false);
                return stream.ToArray();
            }
        }

        private MemoryStream stream;
        private IWorkbook xlPackage;
        public virtual IExportProvider BuilderExportToByte<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport)
        {
            if (stream == null)
                stream = new MemoryStream();

            if (xlPackage == null)
                xlPackage = new XSSFWorkbook();

            ISheet worksheet = xlPackage.CreateSheet(typeof(T).Name);
            var manager = new PropertyManager<T>(properties);
            WriteCaption(worksheet, properties);
            var row = 1;
            foreach (var items in itemsToExport)
            {
                WriteToXlsx(properties, worksheet, items, row++);
            }

            return this;
        }
        public virtual byte[] BuilderExportToByte()
        {
            xlPackage.Write(stream, false);
            return stream.ToArray();
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
        private void WriteToXlsx<T>(PropertyByName<T>[] properties, ISheet sheet, T items, int row)
        {
            IRow _row = sheet.CreateRow(row);
            foreach (var prop in properties)
            {
                var cellValue = (prop.GetProperty(items)?.ToString());
                if (cellValue != null && cellValue.Length >= 32767) // 32767 is the max char size of an excel cell
                {
                    cellValue = cellValue.Substring(0, 32767); //Truncate the content to max size.
                }
                _row.CreateCell(prop.PropertyOrderPosition).SetCellValue(cellValue);
            }
        }
    }
}
