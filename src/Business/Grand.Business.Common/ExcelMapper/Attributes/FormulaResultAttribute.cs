using System;

namespace Ganss.Excel
{
    /// <summary>
    /// Attribute which specifies that the formula result instead of the formula should be mapped.
    /// This applies only to string properties, as for all other types the result will be mapped.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class FormulaResultAttribute : Attribute
    {
    }
}
