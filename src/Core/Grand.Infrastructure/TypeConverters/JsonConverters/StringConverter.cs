using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grand.Infrastructure.TypeConverters.JsonConverters;

public class StringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
            {
                var stringValue = reader.GetInt32();
                return stringValue.ToString();
            }
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            case JsonTokenType.String:
                return reader.GetString();
            default:
                throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}