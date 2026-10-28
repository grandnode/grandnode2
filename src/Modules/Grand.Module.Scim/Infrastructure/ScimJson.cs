using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grand.Module.Scim.Infrastructure;

/// <summary>
///     The JSON of the SCIM endpoint: camelCase, nulls left out, names read case-insensitively. Its own options, never the MVC
///     ones of the application (the admin panels serialize PascalCase)
/// </summary>
public static class ScimJson
{
    public const string ContentType = "application/scim+json";

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>A SCIM boolean: true/false, or the strings "true"/"false" in any letter case (Entra ID sends "False")</summary>
    public static bool TryReadBoolean(JsonElement value, out bool result)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.True:
                result = true;
                return true;
            case JsonValueKind.False:
                result = false;
                return true;
            case JsonValueKind.String:
                return bool.TryParse(value.GetString(), out result);
            default:
                result = false;
                return false;
        }
    }
}

/// <summary>Reads a SCIM boolean (<see cref="ScimJson.TryReadBoolean" />) and writes a JSON boolean</summary>
public class ScimBooleanConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        using var document = JsonDocument.ParseValue(ref reader);
        return ScimJson.TryReadBoolean(document.RootElement, out var result)
            ? result
            : throw new JsonException("Not a boolean");
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteBooleanValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
