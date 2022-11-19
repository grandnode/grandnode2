using Ganss.Excel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Utilities
{
    public class PowerColumnInfo : ColumnInfo
    {
        /// <summary>
        /// Computes value that can be assigned to property from cell value.
        /// </summary>
        /// <param name="o">The object which contains the property.</param>
        /// <param name="val">The value.</param>
        /// <param name="cell">The cell where the value originates from.</param>
        /// <returns>Value that can be assigned to property.</returns>
        public override object GetPropertyValue(object o, object val, ICell cell)
        {
            object v;
            if (SetProp != null)
                v = SetProp(o, val, cell);
            else if (IsNullable && (val == null || val is string s && s.Length == 0) && PropertyType == typeof(string)) //CHANGES FOR GRANDNODE
                v = ""; //CHANGES FOR GRANDNODE
            else if (IsNullable && (val == null || val is string z && z.Length == 0))
                v = null;
            else if (val is string g && PropertyType == typeof(Guid))
                v = Guid.Parse(g);
            else if (val is string es && PropertyType.IsEnum)
                v = ParseEnum(PropertyType, es);
            else if (val is string && PropertyType == typeof(byte[]))
                v = Encoding.UTF8.GetBytes(val as string);
            else
                v = Convert.ChangeType(val, PropertyType, CultureInfo.InvariantCulture);

            return v;
        }

        private object ParseEnum(Type t, string s)
        {
            var name = Enum.GetNames(t).FirstOrDefault(n => n.Equals(s, StringComparison.OrdinalIgnoreCase));
            return name == null ? Activator.CreateInstance(t) : Enum.Parse(t, name);
        }
    }
}
