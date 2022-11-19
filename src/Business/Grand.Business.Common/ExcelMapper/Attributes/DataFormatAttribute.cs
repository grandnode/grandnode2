using System;

namespace Ganss.Excel
{
    /// <summary>
    /// Attribute that specifies the data format of an Excel cell.
    /// The format can either be a <a href="https://poi.apache.org/apidocs/org/apache/poi/ss/usermodel/BuiltinFormats.html">builtin format</a>
    /// or a <a href="https://support.office.com/en-nz/article/Create-or-delete-a-custom-number-format-78f2a361-936b-4c03-8772-09fab54be7f4">custom format string</a>.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class DataFormatAttribute: Attribute
    {
        /// <summary>
        /// Gets or sets the builtin format, see https://poi.apache.org/apidocs/org/apache/poi/ss/usermodel/BuiltinFormats.html for possible values.
        /// </summary>
        /// <value>
        /// The builtin format.
        /// </value>
        public short BuiltinFormat { get; set; }

        /// <summary>
        /// Gets or sets the custom format, see https://support.office.com/en-nz/article/Create-or-delete-a-custom-number-format-78f2a361-936b-4c03-8772-09fab54be7f4 for the syntax.
        /// </summary>
        /// <value>
        /// The custom format.
        /// </value>
        public string CustomFormat { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFormatAttribute"/> class.
        /// </summary>
        /// <param name="format">The format, see https://poi.apache.org/apidocs/org/apache/poi/ss/usermodel/BuiltinFormats.html for possible values.</param>
        public DataFormatAttribute(short format)
        {
            BuiltinFormat = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFormatAttribute"/> class.
        /// </summary>
        /// <param name="format">The format, see https://support.office.com/en-nz/article/Create-or-delete-a-custom-number-format-78f2a361-936b-4c03-8772-09fab54be7f4 for the syntax.</param>
        public DataFormatAttribute(string format)
        {
            CustomFormat = format;
        }
    }
}
