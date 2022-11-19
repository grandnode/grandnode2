using Ganss.Excel.Exceptions;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ganss.Excel
{
    /// <summary>
    /// Map objects to Excel files.
    /// </summary>
    public class ExcelMapper
    {
        /// <summary>
        /// Gets or sets the <see cref="TypeMapper"/> factory.
        /// Default is a static <see cref="Ganss.Excel.TypeMapperFactory"/> object that caches <see cref="TypeMapper"/>s statically across <see cref="ExcelMapper"/> instances.
        /// </summary>
        /// <value>
        /// The <see cref="TypeMapper"/> factory.
        /// </value>
        public ITypeMapperFactory TypeMapperFactory { get; set; } = DefaultTypeMapperFactory;

        /// <summary>
        /// Gets or sets a value indicating whether the Excel file contains a header row of column names. Default is <c>true</c>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the Excel file contains a header row; otherwise, <c>false</c>.
        /// </value>
        public bool HeaderRow { get; set; } = true;

        /// <summary>
        /// Gets or sets the row number of the header row. Default is 0.
        /// The header row may be outside of the range of <see cref="MinRowNumber"/> and <see cref="MaxRowNumber"/>.
        /// </summary>
        /// <value>
        /// The header row number.
        /// </value>
        public int HeaderRowNumber { get; set; } = 0;

        /// <summary>
        /// Gets or sets the minimum row number of the rows that may contain data. Default is 0.
        /// </summary>
        /// <value>
        /// The minimum row number.
        /// </value>
        public int MinRowNumber { get; set; } = 0;

        /// <summary>
        /// Gets or sets the inclusive maximum row number of the rows that may contain data. Default is <see cref="int.MaxValue"/>.
        /// </summary>
        /// <value>
        /// The maximum row number.
        /// </value>
        public int MaxRowNumber { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets or sets a value indicating whether to track objects read from the Excel file. Default is true.
        /// If object tracking is enabled, the <see cref="ExcelMapper"/> object keeps track of objects it yields through the Fetch() methods.
        /// You can then modify these objects and save them back to an Excel file without having to specify the list of objects to save.
        /// </summary>
        /// <value>
        ///   <c>true</c> if object tracking is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool TrackObjects { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to skip blank rows when reading from Excel files. Default is true.
        /// </summary>
        /// <value>
        ///   <c>true</c> if blank lines are skipped; otherwise, <c>false</c>.
        /// </value>
        public bool SkipBlankRows { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to skip blank cells when reading from Excel files. Default is true.
        /// </summary>
        /// <value>
        ///   <c>true</c> if blank lines are skipped; otherwise, <c>false</c>.
        /// </value>
        public bool SkipBlankCells { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to create columns in existing Excel files for properties where
        /// the corresponding header does not yet exist. If this is false and properties are mapped by name,
        /// their corresponding headers must already be present in existing files.
        /// Default is false.
        /// </summary>
        /// <value>
        ///   <c>true</c> if missing headers should be created; otherwise, <c>false</c>.
        /// </value>
        public bool CreateMissingHeaders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore nested types.
        /// Default is false.
        /// </summary>
        /// <value>
        ///   <c>true</c> if nested types should be ignored; otherwise, <c>false</c>.
        /// </value>
        public bool IgnoreNestedTypes { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataFormatter"/> object to use when formatting cell values.
        /// </summary>
        /// <value>
        /// The <see cref="DataFormatter"/> object to use when formatting cell values.
        /// </value>
        public DataFormatter DataFormatter { get; set; } = new DataFormatter(CultureInfo.InvariantCulture);

        /// <summary>
        /// Occurs before saving and allows the workbook to be manipulated.
        /// </summary>
        public event EventHandler<SavingEventArgs> Saving;

        /// <summary>
        /// Occurs while parsing when value is not convertible.
        /// Set Cancel to <c>true</c> to Cancel Exception, also, see <see cref="ParsingErrorEventArgs"/>
        /// </summary>
        public event EventHandler<ParsingErrorEventArgs> ErrorParsingCell;

        private Func<string, string> NormalizeName { get; set; }

        readonly Dictionary<Type, Func<object>> ObjectFactories = new();
        Dictionary<string, Dictionary<int, object>> Objects { get; set; } = new();
        IWorkbook Workbook { get; set; }

        static readonly TypeMapperFactory DefaultTypeMapperFactory = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapper"/> class.
        /// </summary>
        public ExcelMapper() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapper"/> class.
        /// </summary>
        /// <param name="workbook">The workbook.</param>
        public ExcelMapper(IWorkbook workbook)
        {
            Workbook = workbook;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapper"/> class.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        public ExcelMapper(string file)
        {
            Workbook = WorkbookFactory.Create(file);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapper"/> class.
        /// </summary>
        /// <param name="stream">The stream the Excel file is read from.</param>
        public ExcelMapper(Stream stream)
        {
            Workbook = WorkbookFactory.Create(stream);
        }

        /// <summary>
        /// Attaches the Excel file from the provided <see cref="IWorkbook"/>.
        /// </summary>
        /// <param name="workbook">The workbook.</param>
        public void Attach(IWorkbook workbook)
        {
            Workbook = workbook;
        }

        /// <summary>
        /// Attaches the Excel file from the provided path.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        public void Attach(string file)
        {
            Workbook = WorkbookFactory.Create(file);
        }

        /// <summary>
        /// Attaches the Excel file read from the provided <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream the Excel file is read from.</param>
        public void Attach(Stream stream)
        {
            Workbook = WorkbookFactory.Create(stream);
        }

        /// <summary>
        /// Sets a factory function to create objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to create.</typeparam>
        /// <param name="factory">The factory function.</param>
        public void CreateInstance<T>(Func<T> factory)
        {
            ObjectFactories[typeof (T)] = () => factory();
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<T> Fetch<T>(string file, string sheetName, Func<string, object, object> valueParser = null)
        {
            return Fetch(file, typeof(T), sheetName, valueParser).OfType<T>();
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable Fetch(string file, Type type, string sheetName, Func<string, object, object> valueParser = null)
        {
            Workbook = WorkbookFactory.Create(file);
            return Fetch(type, sheetName, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<dynamic> Fetch(string file, string sheetName, Func<string, object, object> valueParser = null)
        {
            Workbook = WorkbookFactory.Create(file);
            return Fetch(sheetName, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<T> Fetch<T>(string file, int sheetIndex, Func<string, object, object> valueParser = null)
        {
            return Fetch(file, typeof(T), sheetIndex, valueParser).OfType<T>();
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable Fetch(string file, Type type, int sheetIndex, Func<string, object, object> valueParser = null)
        {
            Workbook = WorkbookFactory.Create(file);
            return Fetch(type, sheetIndex, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<dynamic> Fetch(string file, int sheetIndex, Func<string, object, object> valueParser = null)
        {
            Workbook = WorkbookFactory.Create(file);
            return Fetch(sheetIndex, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<T> Fetch<T>(Stream stream, string sheetName, Func<string, object, object> valueParser = null)
        {
            return Fetch(stream, typeof(T), sheetName, valueParser).OfType<T>();
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable Fetch(Stream stream, Type type, string sheetName, Func<string, object, object> valueParser = null)
        {
            Workbook = WorkbookFactory.Create(stream);
            return Fetch(type, sheetName, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<dynamic> Fetch(Stream stream, string sheetName, Func<string, object, object> valueParser = null)
        {
            Workbook = WorkbookFactory.Create(stream);
            return Fetch(sheetName, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<T> Fetch<T>(Stream stream, int sheetIndex, Func<string, object, object> valueParser = null)
        {
            return Fetch(stream, typeof(T), sheetIndex, valueParser).OfType<T>();
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable Fetch(Stream stream, Type type, int sheetIndex, Func<string, object, object> valueParser = null)
        {
            Workbook = WorkbookFactory.Create(stream);
            return Fetch(type, sheetIndex, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<dynamic> Fetch(Stream stream, int sheetIndex, Func<string, object, object> valueParser = null)
        {
            Workbook = WorkbookFactory.Create(stream);
            return Fetch(sheetIndex, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a sheet is not found</exception>
        public IEnumerable<T> Fetch<T>(string sheetName, Func<string, object, object> valueParser = null)
        {
            return Fetch(typeof(T), sheetName, valueParser).OfType<T>();
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a sheet is not found</exception>
        public IEnumerable Fetch(Type type, string sheetName, Func<string, object, object> valueParser = null)
        {
            PrimitiveCheck(type);

            var sheet = Workbook.GetSheet(sheetName);
            if (sheet == null)
            {
                throw new ArgumentOutOfRangeException(nameof(sheetName), sheetName, "Sheet not found");
            }
            return Fetch(sheet, type, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet name.
        /// </summary>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a sheet is not found</exception>
        public IEnumerable<dynamic> Fetch(string sheetName, Func<string, object, object> valueParser = null)
        {
            var sheet = Workbook.GetSheet(sheetName);
            if (sheet == null)
            {
                throw new ArgumentOutOfRangeException(nameof(sheetName), sheetName, "Sheet not found");
            }
            return Fetch(sheet, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<T> Fetch<T>(int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            var sheet = Workbook.GetSheetAt(sheetIndex);
            return Fetch<T>(sheet, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable Fetch(Type type, int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            PrimitiveCheck(type);

            var sheet = Workbook.GetSheetAt(sheetIndex);
            return Fetch(sheet, type, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index.
        /// </summary>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public IEnumerable<dynamic> Fetch(int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            var sheet = Workbook.GetSheetAt(sheetIndex);
            return Fetch(sheet, valueParser);
        }

        IEnumerable<T> Fetch<T>(ISheet sheet, Func<string, object, object> valueParser = null)
        {
            return Fetch(sheet, typeof(T), valueParser).OfType<T>();
        }

        IEnumerable Fetch(ISheet sheet, Type type, Func<string, object, object> valueParser = null)
        {
            var firstRowNumber = HeaderRowNumber;

            if (!HeaderRow)
                firstRowNumber = sheet.Rows().Where(r => r.RowNum >= MinRowNumber && r.RowNum <= MaxRowNumber)
                    .OrderByDescending(r => r.LastCellNum).FirstOrDefault()?.RowNum ?? 0;

            var firstRow = sheet.GetRow(firstRowNumber);

            if (firstRow == null)
                yield break;

            var cells = Enumerable.Range(0, firstRow.LastCellNum).Select(i => firstRow.GetCell(i, MissingCellPolicy.RETURN_BLANK_AS_NULL));
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
                    object o = MapCells(type, valueParser, typeMapper, firstRowCells, ref objInstanceIdx, row, new HashSet<Type> { type });
                    yield return o;
                }
            }
        }

        IEnumerable<dynamic> Fetch(ISheet sheet, Func<string, object, object> valueParser = null) =>
            Fetch(sheet, type: null, valueParser).Cast<dynamic>();

        private object MapCells(Type type, Func<string, object, object> valueParser, TypeMapper typeMapper,
            IEnumerable<ICell> cells, ref int objInstanceIdx, IRow row, ISet<Type> callChain)
        {
            var sheet = row.Sheet;
            var i = row.RowNum;
            List<(ColumnInfo Col, object CellValue, ICell Cell, int ColumnIndex)> initValues = new();
            var columns = cells
                .Select(c => (Index: c.ColumnIndex,
                    Columns: GetColumnInfo(typeMapper, c).Where(c => c.Directions.HasFlag(MappingDirections.ExcelToObject) && !c.IsSubType).ToList()))
                .Where(c => c.Columns.Any())
                .ToList();

            foreach (var (columnIndex, columnInfos) in columns)
            {
                var cell = row.GetCell(columnIndex, MissingCellPolicy.CREATE_NULL_AS_BLANK);

                if (cell != null && (!SkipBlankCells || !IsCellBlank(cell)))
                {
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
                            TriggerOrThrowParsingError(new ExcelMapperConvertException(cellValue, ci.PropertyType, i, columnIndex, e));
                        }

                        try
                        {
                            if (valueParser != null)
                                cellValue = valueParser(string.IsNullOrWhiteSpace(ci.Name) ? columnIndex.ToString() : ci.Name, cellValue);

                            initValues.Add((ci, cellValue, cell, columnIndex));
                        }
                        catch (Exception e)
                        {
                            TriggerOrThrowParsingError(new ExcelMapperConvertException(cellValue, ci.PropertyType, i, columnIndex, e));
                        }
                    }
                }
            }

            if (!IgnoreNestedTypes)
            {
                foreach (var ci in typeMapper.ColumnsByName.SelectMany(c => c.Value).Where(c => c.IsSubType))
                {
                    if (!callChain.Contains(ci.PropertyType) // check for cycle in type hierarchy
                        && !initValues.Any(v => v.Col.Property.IsIdenticalTo(ci.Property))) // map subtypes only if not already mapped
                    {
                        callChain.Add(ci.PropertyType);
                        var subTypeMapper = TypeMapperFactory.Create(ci.PropertyType);
                        var subObject = MapCells(ci.PropertyType, valueParser, subTypeMapper, cells, ref objInstanceIdx, row, callChain);
                        initValues.Add((ci, subObject, null, -1));
                    }
                }
            }

            object o;

            if (type == null)
                o = typeMapper.CreateExpando();
            else
            {
                if (typeMapper.Constructor != null)
                {
                    var parms = typeMapper.Constructor.GetParameters();
                    var vals = parms.Select(p => GetDefault(p.ParameterType)).ToArray();

                    foreach (var initVal in initValues.ToList())
                    {
                        if (typeMapper.ConstructorParams.TryGetValue(initVal.Col.Property.Name, out var parm))
                        {
                            try
                            {
                                object v;

                                if (initVal.Cell != null)
                                    v = initVal.Col.GetPropertyValue(null, initVal.CellValue, initVal.Cell);
                                else
                                    v = initVal.CellValue;

                                vals[parm.Position] = v;

                                initValues.Remove(initVal);
                            }
                            catch (Exception ex)
                            {
                                TriggerOrThrowParsingError(new ExcelMapperConvertException(initVal.CellValue, initVal.Col.PropertyType, i, initVal.ColumnIndex, ex));
                            }
                        }
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
                    {
                        o = factory();
                    }
                    else
                    {
                        try
                        {
                            o = Activator.CreateInstance(type);
                        }
                        catch (Exception)
                        {
                            o = null;
                        }
                    }

                    if (o == null)
                        return null;
                }
            }

            if (initValues.Any())
            {
                typeMapper?.BeforeMappingActionInvoker?.Invoke(o, objInstanceIdx);

                foreach (var val in initValues)
                {
                    try
                    {
                        if (val.Cell != null)
                            val.Col.SetProperty(o, val.CellValue, val.Cell);
                        else
                            val.Col.Property.SetValue(o, val.CellValue);
                    }
                    catch (Exception ex)
                    {
                        TriggerOrThrowParsingError(new ExcelMapperConvertException(val.CellValue, val.Col.PropertyType, i, val.ColumnIndex, ex));
                    }
                }
            }
            if (TrackObjects) Objects[sheet.SheetName][i] = o;

            typeMapper?.AfterMappingActionInvoker?.Invoke(o, objInstanceIdx);

            objInstanceIdx++;
            return o;
        }

        private void TriggerOrThrowParsingError(ExcelMapperConvertException excelMapperConvertException)
        {
            var parsingError = new ParsingErrorEventArgs(excelMapperConvertException);
            ErrorParsingCell?.Invoke(this, parsingError);
            if (!parsingError.Cancel)
                throw excelMapperConvertException;
        }

        static object GetDefault(Type t) => t.GetTypeInfo().IsValueType ? Activator.CreateInstance(t) : null;

        /// <summary>
        /// Fetches objects from the specified sheet name using async I/O.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable<T>> FetchAsync<T>(string file, string sheetName, Func<string, object, object> valueParser = null)
        {
            return (await FetchAsync(file, typeof(T), sheetName, valueParser)).OfType<T>();
        }

        /// <summary>
        /// Fetches dynamic objects from the specified sheet name using async I/O.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable<dynamic>> FetchAsync(string file, string sheetName, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(file);
            return Fetch(ms, sheetName, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet name using async I/O.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable> FetchAsync(string file, Type type, string sheetName, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(file);
            return Fetch(ms, type, sheetName, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index using async I/O.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable<T>> FetchAsync<T>(string file, int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(file);
            return Fetch(ms, typeof(T), sheetIndex, valueParser).OfType<T>();
        }

        /// <summary>
        /// Fetches dynamic objects from the specified sheet index using async I/O.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable<dynamic>> FetchAsync(string file, int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(file);
            return Fetch(ms, sheetIndex, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index using async I/O.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable> FetchAsync(string file, Type type, int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(file);
            return Fetch(ms, type, sheetIndex, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet name using async I/O.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable<T>> FetchAsync<T>(Stream stream, string sheetName, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(stream);
            return Fetch(ms, typeof(T), sheetName, valueParser).OfType<T>();
        }

        /// <summary>
        /// Fetches dynamic objects from the specified sheet name using async I/O.
        /// </summary>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable<dynamic>> FetchAsync(Stream stream, string sheetName, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(stream);
            return Fetch(ms, sheetName, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet name using async I/O.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable> FetchAsync(Stream stream, Type type, string sheetName, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(stream);
            return Fetch(ms, type, sheetName, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index using async I/O.
        /// </summary>
        /// <typeparam name="T">The type of objects the Excel file is mapped to.</typeparam>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable<T>> FetchAsync<T>(Stream stream, int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(stream);
            return Fetch(ms, typeof(T), sheetIndex, valueParser).OfType<T>();
        }

        /// <summary>
        /// Fetches dynamic objects from the specified sheet index using async I/O.
        /// </summary>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable<dynamic>> FetchAsync(Stream stream, int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(stream);
            return Fetch(ms, sheetIndex, valueParser);
        }

        /// <summary>
        /// Fetches objects from the specified sheet index using async I/O.
        /// </summary>
        /// <param name="type">The type of objects the Excel file is mapped to.</param>
        /// <param name="stream">The stream the Excel file is read from.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="valueParser">Allow value parsing</param>
        /// <returns>The objects read from the Excel file.</returns>
        public async Task<IEnumerable> FetchAsync(Stream stream, Type type, int sheetIndex = 0, Func<string, object, object> valueParser = null)
        {
            using var ms = await ReadAsync(stream);
            return Fetch(ms, type, sheetIndex, valueParser);
        }

        /// <summary>
        /// Fetches the names of all sheets.
        /// </summary>
        /// <returns>The sheet names.</returns>
        public IEnumerable<string> FetchSheetNames() => Workbook == null ? Array.Empty<string>() : Enumerable.Range(0, Workbook.NumberOfSheets).Select(i => Workbook.GetSheetName(i));

        static async Task<Stream> ReadAsync(string file)
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            var ms = new MemoryStream();
            await fs.CopyToAsync(ms);
            return ms;
        }

        static async Task<Stream> ReadAsync(Stream stream)
        {
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms;
        }

        private static bool IsCellBlank(ICell cell)
        {
            return cell.CellType switch
            {
                CellType.String => string.IsNullOrWhiteSpace(cell.StringCellValue),
                CellType.Blank => true,
                _ => false,
            };
        }

        List<ColumnInfo> GetColumnInfo(TypeMapper typeMapper, ICell cell)
        {
            var colByIndex = typeMapper.GetColumnByIndex(cell.ColumnIndex);

            if (!HeaderRow || colByIndex != null)
                return colByIndex ?? new();

            var name = cell.ToString();
            var normalizedName = NormalizeCellName(typeMapper, name);
            var colByName = typeMapper.GetColumnByName(normalizedName);

            // map column by name only if it hasn't been mapped to another property by index
            if (colByName != null
                && !typeMapper.ColumnsByIndex.SelectMany(ci => ci.Value).Any(c => c.Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return colByName;

            return new();
        }

        private string NormalizeCellName(TypeMapper typeMapper, string name)
        {
            if (typeMapper.NormalizeName != null) return typeMapper.NormalizeName(name);
            else if (NormalizeName != null) return NormalizeName(name);
            return name;
        }

        /// <summary>
        /// Saves the specified objects to the specified Excel file.
        /// </summary>
        /// <typeparam name="T">The type of objects to save.</typeparam>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="objects">The objects to save.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public void Save<T>(string file, IEnumerable<T> objects, string sheetName, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var fs = File.Open(file, FileMode.Create, FileAccess.Write);
            Save(fs, objects, sheetName, xlsx, valueConverter);
        }

        /// <summary>
        /// Saves the specified objects to the specified Excel file.
        /// </summary>
        /// <typeparam name="T">The type of objects to save.</typeparam>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="objects">The objects to save.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public void Save<T>(string file, IEnumerable<T> objects, int sheetIndex = 0, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var fs = File.Open(file, FileMode.Create, FileAccess.Write);
            Save(fs, objects, sheetIndex, xlsx, valueConverter);
        }

        /// <summary>
        /// Saves the specified objects to the specified stream.
        /// </summary>
        /// <typeparam name="T">The type of objects to save.</typeparam>
        /// <param name="stream">The stream to save the objects to.</param>
        /// <param name="objects">The objects to save.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public void Save<T>(Stream stream, IEnumerable<T> objects, string sheetName, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            Workbook ??= xlsx ? (IWorkbook)new XSSFWorkbook() : (IWorkbook)new HSSFWorkbook();
            var sheet = Workbook.GetSheet(sheetName);
            sheet ??= Workbook.CreateSheet(sheetName);
            Save(stream, sheet, objects, valueConverter);
        }

        /// <summary>
        /// Saves the specified objects to the specified stream.
        /// </summary>
        /// <typeparam name="T">The type of objects to save.</typeparam>
        /// <param name="stream">The stream to save the objects to.</param>
        /// <param name="objects">The objects to save.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public void Save<T>(Stream stream, IEnumerable<T> objects, int sheetIndex = 0, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            Workbook ??= xlsx ? (IWorkbook)new XSSFWorkbook() : (IWorkbook)new HSSFWorkbook();
            ISheet sheet;
            if (Workbook.NumberOfSheets > sheetIndex)
                sheet = Workbook.GetSheetAt(sheetIndex);
            else
                sheet = Workbook.CreateSheet();
            Save(stream, sheet, objects, valueConverter);
        }

        /// <summary>
        /// Saves tracked objects to the specified Excel file.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public void Save(string file, string sheetName, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var fs = File.Open(file, FileMode.Create, FileAccess.Write);
            Save(fs, sheetName, xlsx, valueConverter);
        }

        /// <summary>
        /// Saves tracked objects to the specified Excel file.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public void Save(string file, int sheetIndex = 0, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var fs = File.Open(file, FileMode.Create, FileAccess.Write);
            Save(fs, sheetIndex, xlsx, valueConverter);
        }

        /// <summary>
        /// Saves tracked objects to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to save the objects to.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public void Save(Stream stream, string sheetName, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            Workbook ??= xlsx ? (IWorkbook)new XSSFWorkbook() : (IWorkbook)new HSSFWorkbook();
            var sheet = Workbook.GetSheet(sheetName);
            sheet ??= Workbook.CreateSheet(sheetName);
            Save(stream, sheet, valueConverter);
        }

        /// <summary>
        /// Saves tracked objects to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to save the objects to.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public void Save(Stream stream, int sheetIndex = 0, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            Workbook ??= xlsx ? (IWorkbook)new XSSFWorkbook() : (IWorkbook)new HSSFWorkbook();
            var sheet = Workbook.GetSheetAt(sheetIndex);
            sheet ??= Workbook.CreateSheet();
            Save(stream, sheet, valueConverter);
        }

        void Save(Stream stream, ISheet sheet, Func<string, object, object> valueConverter = null)
        {
            var objects = Objects[sheet.SheetName];
            var typeMapper = TypeMapperFactory.Create(objects.First().Value);
            var columnsByIndex = typeMapper.ColumnsByIndex;

            columnsByIndex = columnsByIndex.Where(kvp => !kvp.Value.All(ci => ci.Directions == MappingDirections.ExcelToObject))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            GetColumns(sheet, typeMapper, columnsByIndex);

            SetColumnStyles(sheet, columnsByIndex);

            foreach (var o in objects)
            {
                var i = o.Key;
                var row = sheet.GetRow(i);
                row ??= sheet.CreateRow(i);

                SetCells(typeMapper, columnsByIndex, o.Value, row, valueConverter);
            }

            Saving?.Invoke(this, new SavingEventArgs(sheet));

            Workbook.Write(stream, leaveOpen: true);
        }

        void Save<T>(Stream stream, ISheet sheet, IEnumerable<T> objects, Func<string, object, object> valueConverter = null)
        {
            var firstObject = objects.FirstOrDefault();
            var typeMapper = firstObject is ExpandoObject ? TypeMapperFactory.Create(firstObject) : TypeMapperFactory.Create(typeof(T));
            var columnsByIndex = typeMapper.ColumnsByIndex;
            var i = MinRowNumber;

            columnsByIndex = columnsByIndex.Where(kvp => !kvp.Value.All(ci => ci.Directions == MappingDirections.ExcelToObject))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            GetColumns(sheet, typeMapper, columnsByIndex);

            SetColumnStyles(sheet, columnsByIndex);

            foreach (var o in objects)
            {
                if (i > MaxRowNumber) break;

                if (HeaderRow && i == HeaderRowNumber)
                    i++;

                var row = sheet.GetRow(i);
                row ??= sheet.CreateRow(i);

                SetCells(typeMapper, columnsByIndex, o, row, valueConverter);

                i++;
            }

            if (SkipBlankCells)
            {
                while (i <= sheet.LastRowNum && i <= MaxRowNumber)
                {
                    var row = sheet.GetRow(i);
                    while (row.Cells.Any())
                        row.RemoveCell(row.GetCell(row.FirstCellNum));
                    i++;
                }
            }

            Saving?.Invoke(this, new SavingEventArgs(sheet));

            Workbook.Write(stream, leaveOpen: true);
        }

        private void SetCells(TypeMapper typeMapper,
            Dictionary<int, List<ColumnInfo>> columnsByIndex,
            object o, IRow row,
            Func<string, object, object> valueConverter = null)
        {
            var columnsByName = typeMapper.ColumnsByName;

            foreach (var col in columnsByIndex)
            {
                var cell = row.GetCell(col.Key, MissingCellPolicy.CREATE_NULL_AS_BLANK);

                foreach (var ci in col.Value.Where(c => (c is DynamicColumnInfo)
                    || (c.Directions.HasFlag(MappingDirections.ObjectToExcel)
                        && (c?.Property?.DeclaringType.IsAssignableFrom(typeMapper.Type) == true))))
                {
                    SetCell(valueConverter, o, cell, ci);
                }
            }

            if (!IgnoreNestedTypes)
            {
                foreach (var col in columnsByName.SelectMany(c => c.Value.Where(c => c.Directions.HasFlag(MappingDirections.ObjectToExcel) && c.IsSubType)))
                {
                    var subTypeMapper = TypeMapperFactory.Create(col.PropertyType);
                    var subObject = col.Property.GetValue(o);

                    if (subObject != null)
                    {
                        SetCells(subTypeMapper, columnsByIndex, subObject, row, valueConverter);
                    }
                }
            }
        }

        private static void SetCell<T>(Func<string, object, object> valueConverter, T objInstance, ICell cell, ColumnInfo ci)
        {
            Type oldType = null;
            object val = ci.GetProperty(objInstance);
            if (valueConverter != null)
            {
                val = valueConverter(ci.Name, val);
            }
            //When the value is a dynamic type or has a specified value conversion function, the type may be inconsistent, and the type needs to be changed
            var newType = val?.GetType() ?? ci.PropertyType;
            if (newType != ci.PropertyType)
            {
                oldType = ci.PropertyType;
                ci.ChangeSetterType(newType);
            }
            ci.SetCellStyle(cell);
            ci.SetCell(cell, val);
            if (oldType != null)
                ci.ChangeSetterType(oldType);
        }

        /// <summary>
        /// Saves the specified objects to the specified Excel file using async I/O.
        /// </summary>
        /// <typeparam name="T">The type of objects to save.</typeparam>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="objects">The objects to save.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public async Task SaveAsync<T>(string file, IEnumerable<T> objects, string sheetName, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var ms = new MemoryStream();
            Save(ms, objects, sheetName, xlsx, valueConverter);
            await SaveAsync(file, ms.ToArray());
        }

        /// <summary>
        /// Saves the specified objects to the specified Excel file using async I/O.
        /// </summary>
        /// <typeparam name="T">The type of objects to save.</typeparam>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="objects">The objects to save.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public async Task SaveAsync<T>(string file, IEnumerable<T> objects, int sheetIndex = 0, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var ms = new MemoryStream();
            Save(ms, objects, sheetIndex, xlsx, valueConverter);
            await SaveAsync(file, ms.ToArray());
        }

        /// <summary>
        /// Saves the specified objects to the specified stream using async I/O.
        /// </summary>
        /// <typeparam name="T">The type of objects to save.</typeparam>
        /// <param name="stream">The stream to save the objects to.</param>
        /// <param name="objects">The objects to save.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public async Task SaveAsync<T>(Stream stream, IEnumerable<T> objects, string sheetName, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var ms = new MemoryStream();
            Save(ms, objects, sheetName, xlsx, valueConverter);
            await SaveAsync(stream, ms);
        }

        /// <summary>
        /// Saves the specified objects to the specified stream using async I/O.
        /// </summary>
        /// <typeparam name="T">The type of objects to save.</typeparam>
        /// <param name="stream">The stream to save the objects to.</param>
        /// <param name="objects">The objects to save.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public async Task SaveAsync<T>(Stream stream, IEnumerable<T> objects, int sheetIndex = 0, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var ms = new MemoryStream();
            Save(ms, objects, sheetIndex, xlsx, valueConverter);
            await SaveAsync(stream, ms);
        }

        /// <summary>
        /// Saves tracked objects to the specified Excel file using async I/O.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public async Task SaveAsync(string file, string sheetName, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var ms = new MemoryStream();
            Save(ms, sheetName, xlsx, valueConverter);
            await SaveAsync(file, ms.ToArray());
        }

        /// <summary>
        /// Saves tracked objects to the specified Excel file using async I/O.
        /// </summary>
        /// <param name="file">The path to the Excel file.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public async Task SaveAsync(string file, int sheetIndex = 0, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var ms = new MemoryStream();
            Save(ms, sheetIndex, xlsx, valueConverter);
            await SaveAsync(file, ms.ToArray());
        }

        /// <summary>
        /// Saves tracked objects to the specified stream using async I/O.
        /// </summary>
        /// <param name="stream">The stream to save the objects to.</param>
        /// <param name="sheetName">Name of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public async Task SaveAsync(Stream stream, string sheetName, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var ms = new MemoryStream();
            Save(ms, sheetName, xlsx, valueConverter);
            await SaveAsync(stream, ms);
        }

        /// <summary>
        /// Saves tracked objects to the specified stream using async I/O.
        /// </summary>
        /// <param name="stream">The stream to save the objects to.</param>
        /// <param name="sheetIndex">Index of the sheet.</param>
        /// <param name="xlsx">if set to <c>true</c> saves in .xlsx format; otherwise, saves in .xls format.</param>
        /// <param name="valueConverter">converter receiving property name and value</param>
        public async Task SaveAsync(Stream stream, int sheetIndex = 0, bool xlsx = true, Func<string, object, object> valueConverter = null)
        {
            using var ms = new MemoryStream();
            Save(ms, sheetIndex, xlsx, valueConverter);
            await SaveAsync(stream, ms);
        }

        static async Task SaveAsync(string file, byte[] buf)
        {
            using var fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write);
            await fs.WriteAsync(buf, 0, buf.Length);
        }

        static async Task SaveAsync(Stream stream, MemoryStream ms)
        {
            var buf = ms.ToArray();
            await stream.WriteAsync(buf, 0, buf.Length);
        }

        static void SetColumnStyles(ISheet sheet, Dictionary<int, List<ColumnInfo>> columnsByIndex)
        {
            foreach (var col in columnsByIndex)
                col.Value.Where(c => c.Directions.HasFlag(MappingDirections.ObjectToExcel))
                    .ToList().ForEach(ci => ci.SetColumnStyle(sheet, col.Key));
        }

        void GetColumns(ISheet sheet, TypeMapper typeMapper, Dictionary<int, List<ColumnInfo>> columnsByIndex)
        {
            var callChain = new HashSet<Type> { typeMapper.Type };

            if (HeaderRow)
            {
                var headerRow = sheet.GetRow(HeaderRowNumber);

                if (headerRow == null)
                {
                    headerRow = sheet.CreateRow(HeaderRowNumber);
                    var columnIndex = 0;
                    PopulateHeaderRow(typeMapper, columnsByIndex, headerRow, ref columnIndex, callChain);
                }
                else
                {
                    if (CreateMissingHeaders)
                    {
                        UpdateHeaderRow(typeMapper, columnsByIndex, headerRow, callChain);
                    }
                    else
                    {
                        ReadHeaderRow(typeMapper, columnsByIndex, headerRow, callChain);
                    }
                }
            }
            else
            {
                columnsByIndex.Clear();
                GatherColumnIndexes(typeMapper, columnsByIndex, callChain);
            }
        }

        private void GatherColumnIndexes(TypeMapper typeMapper, Dictionary<int, List<ColumnInfo>> columnsByIndex, ISet<Type> callChain)
        {
            var columnsByName = typeMapper.ColumnsByName;

            foreach (var (index, columns) in typeMapper.ColumnsByIndex.Select(p => (Index: p.Key,
                Columns: p.Value.Where(c => c.Directions != MappingDirections.ExcelToObject && !c.IsSubType))))
            {
                if (!columnsByIndex.TryGetValue(index, out var columnInfos))
                    columnsByIndex[index] = columnInfos = new();
                columnInfos.AddRange(columns);
            }

            if (!IgnoreNestedTypes)
            {
                foreach (var propertyType in columnsByName.SelectMany(c => c.Value.Where(c => c.Directions != MappingDirections.ExcelToObject && c.IsSubType))
                    .Select(c => c.PropertyType))
                {
                    if (!callChain.Contains(propertyType))
                    {
                        callChain.Add(propertyType);
                        var subTypeMapper = TypeMapperFactory.Create(propertyType);
                        GatherColumnIndexes(subTypeMapper, columnsByIndex, callChain);
                    }
                }
            }
        }

        private void UpdateHeaderRow(TypeMapper typeMapper, Dictionary<int, List<ColumnInfo>> columnsByIndex, IRow headerRow, ISet<Type> callChain)
        {
            var columnsByName = typeMapper.ColumnsByName;

            foreach (var col in columnsByName)
            {
                foreach (var columnInfo in col.Value.Where(c => c.Directions != MappingDirections.ExcelToObject))
                {
                    if (!columnInfo.IsSubType)
                    {
                        var columnInfoByIndex = columnsByIndex.FirstOrDefault(c => c.Value.Any(v =>
                            v.Directions != MappingDirections.ObjectToExcel && v.Property.IsIdenticalTo(columnInfo.Property)));
                        var columnIndex = 0;

                        if (columnInfoByIndex.Value == null)
                        {
                            for (; columnIndex < headerRow.LastCellNum; columnIndex++)
                            {
                                var c = headerRow.GetCell(columnIndex, MissingCellPolicy.RETURN_BLANK_AS_NULL);
                                if (c == null || string.IsNullOrEmpty(c.ToString()))
                                    break;
                            }
                        }
                        else
                        {
                            columnIndex = columnInfoByIndex.Key;
                        }

                        var cell = headerRow.GetCell(columnIndex, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                        columnsByIndex[columnIndex] = col.Value;
                        cell.SetCellValue(col.Key);
                    }
                    else if (!IgnoreNestedTypes && !callChain.Contains(columnInfo.PropertyType))
                    {
                        callChain.Add(columnInfo.PropertyType);
                        var subTypeMapper = TypeMapperFactory.Create(columnInfo.PropertyType);
                        UpdateHeaderRow(subTypeMapper, columnsByIndex, headerRow, callChain);
                    }
                }
            }
        }

        private void ReadHeaderRow(TypeMapper typeMapper, Dictionary<int, List<ColumnInfo>> columnsByIndex, IRow headerRow, ISet<Type> callChain)
        {
            foreach (var cols in headerRow.Cells
                .Where(c => !string.IsNullOrWhiteSpace(c.ToString()))
                .Select(c =>
                {
                    var name = c.ToString();
                    var normalizedName = NormalizeCellName(typeMapper, name);
                    var val = new { c.ColumnIndex, ColumnInfo = typeMapper.GetColumnByName(normalizedName), ColumnName = c.ToString() };
                    return val;
                })
                .Where(c => c.ColumnInfo != null))
            {
                var columnIndex = cols.ColumnIndex;
                if (!columnsByIndex.TryGetValue(columnIndex, out var columnInfos))
                    columnsByIndex[columnIndex] = columnInfos = new();

                foreach (var col in cols.ColumnInfo.Where(c => c.Directions != MappingDirections.ExcelToObject && !c.IsSubType))
                {
                    columnInfos.Add(col);
                }
            }

            if (!IgnoreNestedTypes)
            {
                foreach (var columns in typeMapper.ColumnsByName)
                {
                    foreach (var propertyType in columns.Value.Where(c => c.IsSubType && !callChain.Contains(c.PropertyType))
                        .Select(c => c.PropertyType))
                    {
                        callChain.Add(propertyType);
                        var subTypeMapper = TypeMapperFactory.Create(propertyType);
                        ReadHeaderRow(subTypeMapper, columnsByIndex, headerRow, callChain);
                    }
                }
            }
        }

        private void PopulateHeaderRow(TypeMapper typeMapper, Dictionary<int, List<ColumnInfo>> columnsByIndex,
            IRow headerRow, ref int columnIndex, ISet<Type> callChain)
        {
            var typeColumnsByName = typeMapper.ColumnsByName;
            var typeColumnsByIndex = typeMapper.ColumnsByIndex;

            foreach (var columns in typeColumnsByName)
            {
                var noSubTypeColumns = columns.Value.Where(c => !c.IsSubType && c.Directions != MappingDirections.ExcelToObject).ToList();

                if (noSubTypeColumns.Any())
                {
                    var columnIndexes = typeColumnsByIndex.Where(c =>
                        c.Value.Any(v => v.Directions != MappingDirections.ExcelToObject && noSubTypeColumns.Any(n => n.Name == v.Name)))
                        .Select(c => c.Key);

                    if (columnIndexes.Any())
                    {
                        columnIndex = columnIndexes.First();
                    }
                    else
                    {
                        if (!columnsByIndex.TryGetValue(columnIndex, out var columnInfos))
                            columnsByIndex[columnIndex] = noSubTypeColumns;
                        else
                            columnInfos.AddRange(noSubTypeColumns);
                    }

                    var cell = headerRow.GetCell(columnIndex, MissingCellPolicy.CREATE_NULL_AS_BLANK);

                    cell.SetCellValue(columns.Key);

                    columnIndex++;
                }

                if (!IgnoreNestedTypes)
                {
                    foreach (var propertyType in columns.Value.Where(c => c.IsSubType && !callChain.Contains(c.PropertyType))
                        .Select(c => c.PropertyType))
                    {
                        callChain.Add(propertyType);
                        var subTypeMapper = TypeMapperFactory.Create(propertyType);
                        PopulateHeaderRow(subTypeMapper, columnsByIndex, headerRow, ref columnIndex, callChain);
                    }
                }
            }
        }

        object GetCellValue(ICell cell, ColumnInfo targetColumn)
        {
            var formulaResult = cell.CellType == CellType.Formula && (targetColumn.PropertyType != typeof(string) || targetColumn.FormulaResult);
            var cellType = formulaResult ? cell.CachedFormulaResultType : cell.CellType;
            const int maxDate = 2958465; // 31/12/9999

            switch (cellType)
            {
                case CellType.Numeric:
                    if (!formulaResult && targetColumn.PropertyType == typeof(string))
                    {
                        return DataFormatter.FormatCellValue(cell);
                    }
                    else if (cell.NumericCellValue < maxDate && DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue;
                    }
                    else
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
                    else
                        return cell.StringCellValue;
            }
        }

        object GetCellValue(ICell cell)
        {
            return cell.CellType switch
            {
                CellType.Numeric => cell.NumericCellValue,
                CellType.Formula => cell.CellFormula,
                CellType.Boolean => cell.BooleanCellValue,
                CellType.Error => cell.ErrorCellValue,
                CellType.String => cell.StringCellValue,
                CellType.Blank => string.Empty,
                _ => "<unknown>",
            };
        }

        static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object>> propertyExpression)
        {
            var exp = (LambdaExpression)propertyExpression;
            var mExp = (exp.Body.NodeType == ExpressionType.MemberAccess) ?
                (MemberExpression)exp.Body :
                (MemberExpression)((UnaryExpression)exp.Body).Operand;
            return (PropertyInfo)mExp.Member;
        }

        static void PrimitiveCheck(Type type)
        {
            if (type.IsPrimitive || typeof(string).Equals(type) || typeof(object).Equals(type) || Nullable.GetUnderlyingType(type) != null)
            {
                throw new ArgumentException($"{type.Name} can not be used to map an excel because it is a primitive type");
            }
        }

        /// <summary>
        /// Action to call after an object is mapped
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public ExcelMapper AddAfterMapping<T>(Action<T, int> action)
        {
            var typeMapper = TypeMapperFactory.Create(typeof(T));
            typeMapper.AfterMappingActionInvoker = ActionInvoker.CreateInstance(action);
            return this;
        }

        /// <summary>
        /// Action to call before an object is mapped
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public ExcelMapper AddBeforeMapping<T>(Action<T, int> action)
        {
            var typeMapper = TypeMapperFactory.Create(typeof(T));
            typeMapper.BeforeMappingActionInvoker = ActionInvoker.CreateInstance(action);
            return this;
        }

        /// <summary>
        /// Adds a mapping from a column name to a property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="propertyExpression">The property expression.</param>
        public ColumnInfo AddMapping<T>(string columnName, Expression<Func<T, object>> propertyExpression)
        {
            var typeMapper = TypeMapperFactory.Create(typeof(T));
            var prop = GetPropertyInfo(propertyExpression);

            if (!typeMapper.ColumnsByName.ContainsKey(columnName))
                typeMapper.ColumnsByName.Add(columnName, new List<ColumnInfo>());

            var columnInfo = typeMapper.ColumnsByName[columnName].FirstOrDefault(ci => ci.Property.Name == prop.Name);
            if (columnInfo is null)
            {
                columnInfo = new ColumnInfo(prop);
                typeMapper.ColumnsByName[columnName].Add(columnInfo);
            }

            return columnInfo;
        }

        /// <summary>
        /// Adds a mapping from a column index to a property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex">Index of the column.</param>
        /// <param name="propertyExpression">The property expression.</param>
        public ColumnInfo AddMapping<T>(int columnIndex, Expression<Func<T, object>> propertyExpression)
        {
            var typeMapper = TypeMapperFactory.Create(typeof(T));
            var prop = GetPropertyInfo(propertyExpression);
            var idx = columnIndex - 1;

            if (!typeMapper.ColumnsByIndex.ContainsKey(idx))
                typeMapper.ColumnsByIndex.Add(idx, new List<ColumnInfo>());

            var columnInfo = typeMapper.ColumnsByIndex[idx].FirstOrDefault(ci => ci.Property.Name == prop.Name);
            if (columnInfo is null)
            {
                columnInfo = new ColumnInfo(prop);
                typeMapper.ColumnsByIndex[idx].Add(columnInfo);
            }

            return columnInfo;
        }

        /// <summary>
        /// Adds a mapping from a column name to a property.
        /// </summary>
        /// <param name="t">The type that contains the property to map to.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="propertyName">Name of the property.</param>
        public ColumnInfo AddMapping(Type t, string columnName, string propertyName)
        {
            var typeMapper = TypeMapperFactory.Create(t);
            var prop = t.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            if (!typeMapper.ColumnsByName.ContainsKey(columnName))
                typeMapper.ColumnsByName.Add(columnName, new List<ColumnInfo>());

            var columnInfo = typeMapper.ColumnsByName[columnName].FirstOrDefault(ci => ci.Property.Name == prop.Name);
            if (columnInfo is null)
            {
                columnInfo = new ColumnInfo(prop);
                typeMapper.ColumnsByName[columnName].Add(columnInfo);
            }

            return columnInfo;
        }

        /// <summary>
        /// Adds a mapping from a column name to a property.
        /// </summary>
        /// <param name="t">The type that contains the property to map to.</param>
        /// <param name="columnIndex">Index of the column.</param>
        /// <param name="propertyName">Name of the property.</param>
        public ColumnInfo AddMapping(Type t, int columnIndex, string propertyName)
        {
            var typeMapper = TypeMapperFactory.Create(t);
            var prop = t.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            var idx = columnIndex - 1;

            if (!typeMapper.ColumnsByIndex.ContainsKey(idx))
                typeMapper.ColumnsByIndex.Add(idx, new List<ColumnInfo>());

            var columnInfo = typeMapper.ColumnsByIndex[idx].FirstOrDefault(ci => ci.Property.Name == prop.Name);
            if (columnInfo is null)
            {
                columnInfo = new ColumnInfo(prop);
                typeMapper.ColumnsByIndex[idx].Add(columnInfo);
            }

            return columnInfo;
        }

        /// <summary>
        /// Ignores a property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyExpression">The property expression.</param>
        public void Ignore<T>(Expression<Func<T, object>> propertyExpression)
        {
            var typeMapper = TypeMapperFactory.Create(typeof(T));
            var prop = GetPropertyInfo(propertyExpression);

            typeMapper.ColumnsByName.Where(c => c.Value.Any(cc => cc.Property.IsIdenticalTo(prop)))
                .ToList().ForEach(kvp => typeMapper.ColumnsByName.Remove(kvp.Key));
        }

        /// <summary>
        /// Ignores a property.
        /// </summary>
        /// <param name="t">The type that contains the property to map to.</param>
        /// <param name="propertyName">Name of the property.</param>
        public void Ignore(Type t, string propertyName)
        {
            var typeMapper = TypeMapperFactory.Create(t);
            var prop = t.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            typeMapper.ColumnsByName.Where(c => c.Value.Any(cc => cc.Property.IsIdenticalTo(prop)))
                .ToList().ForEach(kvp => typeMapper.ColumnsByName.Remove(kvp.Key));
        }

        /// <summary>
        /// Sets a name normalization function.
        /// This function is used when the <see cref="ExcelMapper"/> object tries to find a property name from a header cell value.
        /// It can be used if the input header cell values may contain a larger number of possible values that can be easily mapped
        /// backed to a single property name through a function, e.g. if the header cell may contain varying amounts of whitespace.
        /// The default is the identity function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="normalizeName">The name normalization function.</param>
        public void NormalizeUsing<T>(Func<string, string> normalizeName)
        {
            var typeMapper = TypeMapperFactory.Create(typeof(T));
            typeMapper.NormalizeName = normalizeName;
        }

        /// <summary>
        /// Sets a name normalization function.
        /// This function is used when the <see cref="ExcelMapper"/> object tries to find a property name from a header cell value.
        /// It can be used if the input header cell values may contain a larger number of possible values that can be easily mapped
        /// backed to a single property name through a function, e.g. if the header cell may contain varying amounts of whitespace.
        /// The default is the identity function.
        /// </summary>
        /// <param name="t">The type that contains the property to map to.</param>
        /// <param name="normalizeName">The name normalization function.</param>
        public void NormalizeUsing(Type t, Func<string, string> normalizeName)
        {
            var typeMapper = TypeMapperFactory.Create(t);
            typeMapper.NormalizeName = normalizeName;
        }

        /// <summary>
        /// Sets a default name normalization function.
        /// This function is used when the <see cref="ExcelMapper"/> object tries to find a property name from a header cell value.
        /// It can be used if the input header cell values may contain a larger number of possible values that can be easily mapped
        /// backed to a single property name through a function, e.g. if the header cell may contain varying amounts of whitespace.
        /// This default normalization function works across types. If a normalization function is set for a specific type it takes
        /// precedence over this default function.
        /// The default is the identity function.
        /// </summary>
        /// <param name="normalizeName">The name normalization function.</param>
        public void NormalizeUsing(Func<string, string> normalizeName)
        {
            NormalizeName = normalizeName;
        }

        internal static readonly Regex ColumnLetterRegex = new("^$?[A-Z]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Converts Excel column letters to column indexes (e.g. "A" yields 1).
        /// </summary>
        /// <param name="letter">The Excel column letter.</param>
        /// <returns>The column index.</returns>
        public static int LetterToIndex(string letter)
        {
            if (letter == null || !ColumnLetterRegex.IsMatch(letter))
                throw new ArgumentException($"Column letters out of range: {letter}", nameof(letter));
            return CellReference.ConvertColStringToIndex(letter) + 1;
        }

        /// <summary>
        /// Converts a column index to the corresponding Excel column letter or letters (e.g. 1 yields "A").
        /// </summary>
        /// <param name="index">The column index.</param>
        /// <returns>The Excel column letter or letters.</returns>
        public static string IndexToLetter(int index)
        {
            if (index < 1)
                throw new ArgumentException($"Column index out of range: {index}", nameof(index));
            return CellReference.ConvertNumToColString(index - 1);
        }
    }
}
