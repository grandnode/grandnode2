using Grand.Domain.Security;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Grand.Infrastructure.TypeConverters.Converter;

public class RefreshTokenTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string valueStr) return base.ConvertFrom(context, culture, value);
        RefreshToken refreshToken = null;
        if (string.IsNullOrEmpty(valueStr)) return null;
        try
        {
            refreshToken = JsonSerializer.Deserialize<RefreshToken>(valueStr);
        }
        catch
        {
            //deserialize error
        }

        return refreshToken;
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        if (destinationType != typeof(string)) return base.ConvertTo(context, culture, value, destinationType);
        if (value is RefreshToken refreshToken) return JsonSerializer.Serialize(refreshToken);

        return "";
    }
}