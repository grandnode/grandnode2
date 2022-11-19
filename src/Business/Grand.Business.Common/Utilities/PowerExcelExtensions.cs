using NPOI.SS.UserModel;
using System.Reflection;

namespace Grand.Business.Common.Utilities
{
    static class PowerExcelExtensions
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
        /// convert to Nullable type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Type ConvertToNullableType(this Type type)
        {
            if (type.IsPrimitive || type.IsValueType)
            {
                return typeof(Nullable<>).MakeGenericType(type);
            }
            return type;
        }
    }
}
