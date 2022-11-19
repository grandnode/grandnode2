using System;

namespace Ganss.Excel
{
    /// <summary>
    /// Attribute which specifies that the property contains a formula.
    /// This applies only to string properties and is only needed when saving Excel files.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class FormulaAttribute : Attribute
    {
    }
}
