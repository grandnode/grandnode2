using System;

namespace Ganss.Excel
{

    /// <summary>
    /// Data direction
    /// </summary>
    [Flags]
    public enum MappingDirections
    {
        /// <summary>
        /// From Excel to Object
        /// </summary>
        ExcelToObject = 1 << 0,
        /// <summary>
        /// From Object to Excel
        /// </summary>
        ObjectToExcel = 1 << 1,
        /// <summary>
        /// Both directions
        /// </summary>
        Both = ExcelToObject | ObjectToExcel,
    }
}
