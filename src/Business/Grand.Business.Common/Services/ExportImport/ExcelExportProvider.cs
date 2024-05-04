using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Grand.Business.Common.Services.ExportImport;

public class ExcelExportProvider : IExportProvider
{
    private MemoryStream _stream;
    private IWorkbook _xlPackage;

    public virtual byte[] ExportToByte<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport)
    {
        using var stream = new MemoryStream();
        IWorkbook xlPackage = new XSSFWorkbook();
        var worksheet = xlPackage.CreateSheet(typeof(T).Name);

        _ = new PropertyManager<T>(properties);

        WriteCaption(worksheet, properties);

        var row = 1;

        foreach (var items in itemsToExport) WriteToXlsx(properties, worksheet, items, row++);
        xlPackage.Write(stream, false);
        return stream.ToArray();
    }

    public virtual IExportProvider BuilderExportToByte<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport)
    {
        _stream ??= new MemoryStream();

        _xlPackage ??= new XSSFWorkbook();

        var worksheet = _xlPackage.CreateSheet(typeof(T).Name);

        _ = new PropertyManager<T>(properties);

        WriteCaption(worksheet, properties);
        var row = 1;
        foreach (var items in itemsToExport) WriteToXlsx(properties, worksheet, items, row++);

        return this;
    }

    public virtual byte[] BuilderExportToByte()
    {
        _xlPackage.Write(_stream, false);
        return _stream.ToArray();
    }

    /// <summary>
    ///     Write caption (first row) to XLSX worksheet
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="properties"></param>
    private void WriteCaption<T>(ISheet sheet, PropertyByName<T>[] properties)
    {
        var row = sheet.CreateRow(0);
        foreach (var caption in properties)
            row.CreateCell(caption.PropertyOrderPosition).SetCellValue(caption.PropertyName);
    }

    /// <summary>
    ///     Write object data to XLSX worksheet
    /// </summary>
    /// <param name="items"></param>
    /// <param name="idrow">Row index</param>
    /// <param name="properties"></param>
    /// <param name="sheet"></param>
    private void WriteToXlsx<T>(PropertyByName<T>[] properties, ISheet sheet, T items, int idrow)
    {
        var row = sheet.CreateRow(idrow);
        foreach (var prop in properties)
        {
            var cellValue = prop.GetProperty(items)?.ToString();
            if (cellValue is { Length: >= 32767 }) // 32767 is the max char size of an excel cell
                cellValue = cellValue[..32767]; //Truncate the content to max size.
            row.CreateCell(prop.PropertyOrderPosition).SetCellValue(cellValue);
        }
    }
}