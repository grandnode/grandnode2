using Grand.Domain.Shipping;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Grand.Infrastructure.TypeConverters.Converter;

public class ShippingOptionListTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string valueStr) return base.ConvertFrom(context, culture, value);
        List<ShippingOption> shippingOptions = null;
        if (string.IsNullOrEmpty(valueStr)) return null;
        try
        {
            shippingOptions = JsonSerializer.Deserialize<List<ShippingOption>>(valueStr);
        }
        catch
        {
            //xml error
        }

        return shippingOptions;
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        if (destinationType == typeof(string))
            return value is List<ShippingOption> shippingOptions ? JsonSerializer.Serialize(shippingOptions) : "";

        return base.ConvertTo(context, culture, value, destinationType);
    }
}