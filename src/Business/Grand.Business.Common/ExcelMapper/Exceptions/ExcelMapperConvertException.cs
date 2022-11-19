using System;
using System.Runtime.Serialization;

namespace Ganss.Excel.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when conversion of a cell value to the mapped property type fails.
    /// </summary>
    [Serializable]
    public class ExcelMapperConvertException : Exception
    {
        /// <summary>
        /// Gets the original cell value.
        /// </summary>
        /// <value>
        /// The original cell value.
        /// </value>
        public object CellValue { get; }

        /// <summary>
        /// Gets the type of the property that the cell is mapped to.
        /// </summary>
        /// <value>
        /// The type of the property that the cell is mapped to.
        /// </value>
        public Type TargetType { get; }

        /// <summary>
        /// Gets the line number of the cell where the error occurred.
        /// </summary>
        /// <value>
        /// The line number of the cell where the error occurred.
        /// </value>
        public int Line { get; }

        /// <summary>
        /// Gets the column number of the cell where the error occurred.
        /// </summary>
        /// <value>
        /// The column number of the cell where the error occurred.
        /// </value>
        public int Column { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapperConvertException"/> class.
        /// </summary>
        public ExcelMapperConvertException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapperConvertException"/> class.
        /// </summary>
        /// <param name="message">The message the describes the error.</param>
        public ExcelMapperConvertException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapperConvertException"/> class.
        /// </summary>
        /// <param name="message">The message the describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ExcelMapperConvertException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapperConvertException"/> class.
        /// </summary>
        /// <param name="cellValue">The value of the cell where the error occurred.</param>
        /// <param name="targetType">The type of the property the cell is mapped to.</param>
        /// <param name="line">The line number of the cell where the error occurred.</param>
        /// <param name="column">The column number of the cell where the error occurred.</param>
        public ExcelMapperConvertException(object cellValue, Type targetType, int line, int column) : base(FormatMessage(cellValue, targetType, line, column))
        {
            CellValue = cellValue;
            TargetType = targetType;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapperConvertException"/> class.
        /// </summary>
        /// <param name="cellValue">The value of the cell where the error occurred.</param>
        /// <param name="targetType">The type of the property the cell is mapped to.</param>
        /// <param name="line">The line number of the cell where the error occurred.</param>
        /// <param name="column">The column number of the cell where the error occurred.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ExcelMapperConvertException(object cellValue, Type targetType, int line, int column, Exception innerException)
            : this(FormatMessage(cellValue, targetType, line, column), innerException)
        {
            CellValue = cellValue;
            TargetType = targetType;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelMapperConvertException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected ExcelMapperConvertException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Line = info.GetInt32("Line");
            Column = info.GetInt32("Column");
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("Line", Line);
            info.AddValue("Column", Column);

            base.GetObjectData(info, context);
        }

        private static string FormatMessage(object cellValue, Type targetType, int line, int column)
            => $"Unable to convert \"{(cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()) ? "<EMPTY>" : cellValue)}\" from [L:{line}]:[C:{column}] to {targetType}.";
    }
}