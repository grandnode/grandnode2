using System;

namespace Ganss.Excel
{
    /// <summary>
    /// Attribute that specifies that the property should be serialized as JSON.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class JsonAttribute : Attribute
    {
    }
}
