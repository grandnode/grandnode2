using Ganss.Excel;
using Ganss.Excel.Exceptions;
using NPOI.SS.UserModel;
using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace Grand.Business.Common.Utilities;

public class PowerExcelMapper : ExcelMapper
{
    private readonly Dictionary<Type, Func<object>> ObjectFactories = new();

    private Dictionary<string, Dictionary<int, object>> Objects { get; } = new();
    private IWorkbook Workbook { get; set; }

    private static async Task<Stream> ReadAsync(Stream stream)
    {
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms;
    }

    public new async Task<IEnumerable<T>> FetchAsync<T>(Stream stream, int sheetIndex = 0,
        Func<string, object, object> valueParser = null)
    {
        using var ms = await ReadAsync(stream);
        return Fetch(ms, typeof(T), sheetIndex, valueParser).OfType<T>();
    }

    public new IEnumerable Fetch(Stream stream, Type type, int sheetIndex,
        Func<string, object, object> valueParser = null)
    {
        Workbook = WorkbookFactory.Create(stream);
        return Fetch(type, sheetIndex, valueParser);
    }

    public new IEnumerable Fetch(Type type, int sheetIndex = 0, Func<string, object, object> valueParser = null)
    {
        var sheet = Workbook.GetSheetAt(sheetIndex);
        return Fetch(sheet, type, valueParser);
    }

    private IEnumerable Fetch(ISheet sheet, Type type, Func<string, object, object> valueParser = null)
    {
        var firstRowNumber = HeaderRowNumber;

        if (!HeaderRow)
            firstRowNumber = sheet.Rows().Where(r => r.RowNum >= MinRowNumber && r.RowNum <= MaxRowNumber)
                .OrderByDescending(r => r.LastCellNum).FirstOrDefault()?.RowNum ?? 0;

        var firstRow = sheet.GetRow(firstRowNumber);

        if (firstRow == null)
            yield break;

        var cells = Enumerable.Range(0, firstRow.LastCellNum)
            .Select(i => firstRow.GetCell(i, MissingCellPolicy.RETURN_BLANK_AS_NULL));
        var firstRowCells = cells
            .Where(c => !HeaderRow || !string.IsNullOrWhiteSpace(c.ToString()));
        var typeMapper = type != null ? TypeMapperFactory.Create(type) : TypeMapper.Create(firstRowCells, HeaderRow);

        if (TrackObjects) Objects[sheet.SheetName] = new Dictionary<int, object>();

        var objInstanceIdx = 0;

        foreach (IRow row in sheet)
        {
            var i = row.RowNum;

            if (i < MinRowNumber) continue;
            if (i > MaxRowNumber) break;

            // optionally skip header row and blank rows
            if ((!HeaderRow || i != HeaderRowNumber) && (!SkipBlankRows || row.Cells.Any(c => !IsCellBlank(c))))
            {
                var o = MapCells(type, valueParser, typeMapper, firstRowCells, ref objInstanceIdx, row,
                    new HashSet<Type> { type });
                yield return o;
            }
        }
    }

    private static bool IsCellBlank(ICell cell)
    {
        return cell.CellType switch {
            CellType.String => string.IsNullOrWhiteSpace(cell.StringCellValue),
            CellType.Blank => true,
            _ => false
        };
    }

    private object MapCells(Type type, Func<string, object, object> valueParser, TypeMapper typeMapper,
        IEnumerable<ICell> cells, ref int objInstanceIdx, IRow row, ISet<Type> callChain)
    {
        var sheet = row.Sheet;
        var i = row.RowNum;
        List<(ColumnInfo Col, object CellValue, ICell Cell, int ColumnIndex)> initValues = new();
        var columns = cells
            .Select(c => (Index: c.ColumnIndex,
                Columns: GetColumnInfo(typeMapper, c)
                    .Where(c => c.Directions.HasFlag(MappingDirections.ExcelToObject) && !c.IsSubType).ToList()))
            .Where(c => c.Columns.Any())
            .ToList();

        foreach (var (columnIndex, columnInfos) in columns)
        {
            var cell = row.GetCell(columnIndex, MissingCellPolicy.CREATE_NULL_AS_BLANK);

            if (cell != null && (!SkipBlankCells || !IsCellBlank(cell)))
                foreach (var ci in columnInfos)
                {
                    object cellValue;

                    try
                    {
                        cellValue = GetCellValue(cell, ci);
                    }
                    catch (Exception e)
                    {
                        cellValue = GetCellValue(cell);
                        TriggerOrThrowParsingError(new ExcelMapperConvertException(cellValue, ci.PropertyType, i,
                            columnIndex, e));
                    }

                    try
                    {
                        if (valueParser != null)
                            cellValue = valueParser(
                                string.IsNullOrWhiteSpace(ci.Name) ? columnIndex.ToString() : ci.Name, cellValue);

                        initValues.Add((ci, cellValue, cell, columnIndex));
                    }
                    catch (Exception e)
                    {
                        TriggerOrThrowParsingError(new ExcelMapperConvertException(cellValue, ci.PropertyType, i,
                            columnIndex, e));
                    }
                }
        }

        if (!IgnoreNestedTypes)
            foreach (var ci in typeMapper.ColumnsByName.SelectMany(c => c.Value).Where(c => c.IsSubType))
                if (!callChain.Contains(ci.PropertyType) // check for cycle in type hierarchy
                    && !initValues.Any(v =>
                        v.Col.Property.IsIdenticalTo(ci.Property))) // map subtypes only if not already mapped
                {
                    callChain.Add(ci.PropertyType);
                    var subTypeMapper = TypeMapperFactory.Create(ci.PropertyType);
                    var subObject = MapCells(ci.PropertyType, valueParser, subTypeMapper, cells, ref objInstanceIdx,
                        row, callChain);
                    initValues.Add((ci, subObject, null, -1));
                }

        object o;

        if (type == null)
        {
            o = typeMapper.CreateExpando();
        }
        else
        {
            if (typeMapper.Constructor != null)
            {
                var parms = typeMapper.Constructor.GetParameters();
                var vals = parms.Select(p => GetDefault(p.ParameterType)).ToArray();

                foreach (var initVal in initValues.ToList())
                    if (typeMapper.ConstructorParams.TryGetValue(initVal.Col.Property.Name, out var parm))
                        try
                        {
                            object v;

                            if (initVal.Cell != null)
                                v = PowerExcelExtensions.GetPropertyValue(initVal.Col, null, initVal.CellValue,
                                    initVal.Cell);
                            else
                                v = initVal.CellValue;

                            vals[parm.Position] = v;

                            initValues.Remove(initVal);
                        }
                        catch (Exception ex)
                        {
                            TriggerOrThrowParsingError(new ExcelMapperConvertException(initVal.CellValue,
                                initVal.Col.PropertyType, i, initVal.ColumnIndex, ex));
                        }

                try
                {
                    o = typeMapper.Constructor.Invoke(vals);
                }
                catch (Exception ex)
                {
                    throw new ExcelMapperConvertException($"Failed to initialize type {type.FullName}.", ex);
                }
            }
            else
            {
                if (ObjectFactories.TryGetValue(type, out var factory))
                    o = factory();
                else
                    try
                    {
                        o = Activator.CreateInstance(type);
                    }
                    catch (Exception)
                    {
                        o = null;
                    }

                if (o == null)
                    return null;
            }
        }

        if (initValues.Any())
            foreach (var val in initValues)
                try
                {
                    if (val.Cell != null)
                        PowerExcelExtensions.SetProperty(val.Col, o, val.CellValue, val.Cell);
                    else
                        val.Col.Property.SetValue(o, val.CellValue);
                }
                catch (Exception ex)
                {
                    TriggerOrThrowParsingError(new ExcelMapperConvertException(val.CellValue, val.Col.PropertyType, i,
                        val.ColumnIndex, ex));
                }

        if (TrackObjects) Objects[sheet.SheetName][i] = o;

        objInstanceIdx++;
        return o;
    }

    private static object GetDefault(Type t)
    {
        return t.GetTypeInfo().IsValueType ? Activator.CreateInstance(t) : null;
    }

    private void TriggerOrThrowParsingError(ExcelMapperConvertException excelMapperConvertException)
    {
        var parsingError = new ParsingErrorEventArgs(excelMapperConvertException);
        //ErrorParsingCell?.Invoke(this, parsingError);
        if (!parsingError.Cancel)
            throw excelMapperConvertException;
    }

    private List<ColumnInfo> GetColumnInfo(TypeMapper typeMapper, ICell cell)
    {
        var colByIndex = typeMapper.GetColumnByIndex(cell.ColumnIndex);

        if (!HeaderRow || colByIndex != null)
            return colByIndex ?? new List<ColumnInfo>();

        var name = cell.ToString();
        var colByName = typeMapper.GetColumnByName(name);

        // map column by name only if it hasn't been mapped to another property by index
        if (colByName != null
            && !typeMapper.ColumnsByIndex.SelectMany(ci => ci.Value)
                .Any(c => c.Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return colByName;

        return new List<ColumnInfo>();
    }

    private object GetCellValue(ICell cell, ColumnInfo targetColumn)
    {
        var formulaResult = cell.CellType == CellType.Formula &&
                            (targetColumn.PropertyType != typeof(string) || targetColumn.FormulaResult);
        var cellType = formulaResult ? cell.CachedFormulaResultType : cell.CellType;
        const int maxDate = 2958465; // 31/12/9999

        switch (cellType)
        {
            case CellType.Numeric:
                if (!formulaResult && targetColumn.PropertyType == typeof(string))
                    return DataFormatter.FormatCellValue(cell);

                if (cell.NumericCellValue < maxDate && DateUtil.IsCellDateFormatted(cell)) return cell.DateCellValue;
                return cell.NumericCellValue;
            case CellType.Formula:
                return cell.CellFormula;
            case CellType.Boolean:
                return cell.BooleanCellValue;
            case CellType.Error:
                return cell.ErrorCellValue;
            case CellType.Unknown:
            case CellType.Blank:
            case CellType.String:
            default:
                if (targetColumn.Json)
                    return JsonSerializer.Deserialize(cell.StringCellValue, targetColumn.PropertyType);
                return cell.StringCellValue;
        }
    }

    private object GetCellValue(ICell cell)
    {
        return cell.CellType switch {
            CellType.Numeric => cell.NumericCellValue,
            CellType.Formula => cell.CellFormula,
            CellType.Boolean => cell.BooleanCellValue,
            CellType.Error => cell.ErrorCellValue,
            CellType.String => cell.StringCellValue,
            CellType.Blank => string.Empty,
            _ => "<unknown>"
        };
    }
}