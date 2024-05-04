using Grand.Domain.Common;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Grand.Infrastructure.TypeConverters.Converter;

public class CustomAttributeListTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string valueStr) return base.ConvertFrom(context, culture, value);
        List<CustomAttribute> customAttributes = null;
        if (string.IsNullOrEmpty(valueStr)) return null;
        try
        {
            customAttributes = JsonSerializer.Deserialize<List<CustomAttribute>>(valueStr);
        }
        catch
        {
            // ignored
        }

        return customAttributes;
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        if (destinationType == typeof(string))
            return value is List<CustomAttribute> customAttributes ? JsonSerializer.Serialize(customAttributes) : "";

        return base.ConvertTo(context, culture, value, destinationType);
    }
}