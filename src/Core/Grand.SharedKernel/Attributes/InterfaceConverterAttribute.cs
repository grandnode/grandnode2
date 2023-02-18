using System.Text.Json.Serialization;

namespace Grand.SharedKernel.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class InterfaceConverterAttribute : JsonConverterAttribute
{
    public InterfaceConverterAttribute(Type converterType)
        : base(converterType)
    {
    }
}