using NPOI.SS.Util;
using System;

namespace Ganss.Excel
{
    /// <summary>
    /// Attribute that specifies the mapping of a property to a column in an Excel file.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class ColumnAttribute : Attribute
    {
        readonly string name = null;
        int index = -1;
        readonly MappingDirections directions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="directions">mapping direction</param>
        public ColumnAttribute(string name, MappingDirections directions = MappingDirections.Both)
        {
            this.name = name;
            this.directions = directions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnAttribute"/> class.
        /// </summary>
        /// <param name="index">The index of the column.</param>
        /// <param name="directions">mapping direction</param>
        public ColumnAttribute(int index, MappingDirections directions = MappingDirections.Both)
        {
            this.index = index;
            this.directions = directions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnAttribute"/> class.
        /// </summary>
        /// <param name="index">The index of the column.</param>
        /// <param name="name">The name of the column.</param>
        /// <param name="directions">mapping direction</param>
        public ColumnAttribute(int index, string name, MappingDirections directions = MappingDirections.Both)
        {
            this.index = index;
            this.name = name;
            this.directions = directions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnAttribute"/> class.
        /// </summary>
        /// <param name="directions">mapping direction</param>
        public ColumnAttribute(MappingDirections directions = MappingDirections.Both)
        {
            this.directions = directions;
        }

        /// <summary>
        /// Gets the direction of the column.
        /// </summary>
        /// <value>
        /// The name of the column.
        /// </value>
        public MappingDirections Directions => directions;

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        /// <value>
        /// The name of the column.
        /// </value>
        public string Name => name;

        /// <summary>
        /// Gets the index of the column.
        /// </summary>
        /// <value>
        /// The index of the column.
        /// </value>
        public int Index => index;

        /// <summary>
        /// Gets or sets the column name in Excel letter format where A refers to column 1 etc.
        /// </summary>
        public string Letter
        {
            get => ExcelMapper.IndexToLetter(Index);
            set => index = ExcelMapper.LetterToIndex(value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this attribute is inherited by derived classes.
        /// It is useful if you want to prevent multiple mappings created by attributes in derived classes.
        /// The default value is true.
        /// </summary>
        public bool Inherit { get; set; } = true;
    }
}
