using Ganss.Excel;
using NPOI.SS.UserModel;
using System.Globalization;
using System.Reflection;

namespace Grand.Business.Common.Utilities;

internal static class PowerExcelExtensions
{
    internal static IEnumerable<IRow> Rows(this ISheet sheet)
    {
        var e = sheet.GetRowEnumerator();
        while (e.MoveNext())
            yield return e.Current as IRow;
    }

    internal static bool IsIdenticalTo(this MemberInfo memberInfo, MemberInfo other)
    {
        if (memberInfo == null || other == null) return false;
        return memberInfo.Equals(other) || memberInfo.MetadataToken == other.MetadataToken;
    }

    /// <summary>
    ///     convert to Nullable type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static Type ConvertToNullableType(this Type type)
    {
        if (type.IsPrimitive || type.IsValueType) return typeof(Nullable<>).MakeGenericType(type);
        return type;
    }

    internal static object GetPropertyValue(ColumnInfo columnInfo, object o, object val, ICell cell)
    {
        object v;
        if (columnInfo.SetProp != null)
            v = columnInfo.SetProp(o, val, cell);
        else if (columnInfo.IsNullable && (val == null || val is string { Length: 0 }) &&
                 columnInfo.PropertyType == typeof(string)) //CHANGES FOR GRANDNODE
            v = "";
        else if (columnInfo.IsNullable && (val == null || val is string { Length: 0 }))
            v = null;
        else if (val is string g && columnInfo.PropertyType == typeof(Guid))
            v = Guid.Parse(g);
        else if (val is string es && columnInfo.PropertyType.IsEnum)
            v = ParseEnum(columnInfo.PropertyType, es);
        else if (val is string && columnInfo.PropertyType == typeof(byte[]))
            v = Encoding.UTF8.GetBytes(val as string);
        else
            v = Convert.ChangeType(val, columnInfo.PropertyType, CultureInfo.InvariantCulture);

        return v;
    }

    private static object ParseEnum(Type t, string s)
    {
        var name = Enum.GetNames(t).FirstOrDefault(n => n.Equals(s, StringComparison.OrdinalIgnoreCase));
        return name == null ? Activator.CreateInstance(t) : Enum.Parse(t, name);
    }

    /// <summary>
    ///     Sets the property of the specified object to the specified value.
    /// </summary>
    /// <param name="columnInfo"></param>
    /// <param name="o">The object whose property to set.</param>
    /// <param name="val">The value.</param>
    /// <param name="cell">The cell where the value originates from.</param>
    internal static void SetProperty(ColumnInfo columnInfo, object o, object val, ICell cell)
    {
        var v = GetPropertyValue(columnInfo, o, val, cell);
        columnInfo.Property.SetValue(o, v, null);
    }
}