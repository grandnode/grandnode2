using System.Text.Json;

namespace Grand.Infrastructure.TypeConverters.JsonConverters;

public class StringConverter : System.Text.Json.Serialization.JsonConverter<string>
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
                throw new System.Text.Json.JsonException();
        }
    }
 
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
 
}