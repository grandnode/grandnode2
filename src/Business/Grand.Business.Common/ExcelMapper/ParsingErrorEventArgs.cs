using Ganss.Excel.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ganss.Excel
{
    /// <summary>
    /// Provides data for the <see cref="ExcelMapper.ErrorParsingCell"/> event.
    /// Event handler can allow parsing to continue by setting <see cref="CancelEventArgs.Cancel"/> to true,
    /// cancelling the exception.
    /// </summary>
    public class ParsingErrorEventArgs : CancelEventArgs
    {
        /// <summary>
        /// The error captured
        /// </summary>
        public ExcelMapperConvertException Error { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingErrorEventArgs"/> class.
        /// </summary>
        /// <param name="error">The error captured.</param>
        public ParsingErrorEventArgs(ExcelMapperConvertException error)
        {
            Error = error;
        }
    }
}
