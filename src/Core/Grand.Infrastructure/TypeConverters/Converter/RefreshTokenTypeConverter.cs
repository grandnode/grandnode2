using Grand.Domain.Security;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Grand.Infrastructure.TypeConverters.Converter
{
    public class RefreshTokenTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                RefreshToken refreshToken = null;
                var valueStr = value as string;
                if (!string.IsNullOrEmpty(valueStr))
                {
                    try
                    {
                        refreshToken = JsonSerializer.Deserialize<RefreshToken>(valueStr);
                    }
                    catch
                    {
                        //deserialize error
                    }
                }
                return refreshToken;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value is RefreshToken refreshToken)
                {
                    return JsonSerializer.Serialize(refreshToken);
                }

                return "";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
