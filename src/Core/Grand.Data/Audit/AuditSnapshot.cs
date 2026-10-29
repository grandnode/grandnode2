using Grand.Domain;
using Grand.Domain.Configuration;
using MongoDB.Bson;

namespace Grand.Data.Audit;

/// <summary>
///     The stored state of an entity as "path → value", so the audit code above the data layer never handles BSON.
///     An array element is keyed by its id, else by its key (user fields, once per store), else by position.
/// </summary>
public static class AuditSnapshot
{
    /// <summary>
    ///     The value of every binary field: the content is not compared, a written binary field is listed as changed
    /// </summary>
    public const string Binary = "<binary>";

    /// <summary>
    ///     The field holding a setting's values as JSON, flattened field by field when it parses
    /// </summary>
    public const string SettingMetadata = "Metadata";

    public static IDictionary<string, string> Take<T>(T entity) where T : BaseEntity
    {
        return Take(entity, typeof(T));
    }

    public static IDictionary<string, string> Take(object entity, Type entityType)
    {
        return Flatten(entity.ToBsonDocument(entityType), entityType == typeof(Setting));
    }

    public static IDictionary<string, string> Flatten(BsonDocument document, bool isSetting)
    {
        if (document == null)
            return null;

        var leaves = new Dictionary<string, string>();
        foreach (var element in document.Where(e => e.Name != "_id"))
        {
            var value = element.Value;
            if (isSetting && element.Name == SettingMetadata && value.IsString)
                value = TryParse(value.AsString) ?? value;

            Flatten(element.Name, value, leaves);
        }

        return leaves;
    }

    private static void Flatten(string path, BsonValue value, Dictionary<string, string> leaves)
    {
        switch (value)
        {
            case BsonNull or BsonArray { Count: 0 }:
                break;
            //an element's id is already in its path
            case BsonDocument document:
                foreach (var element in document.Where(e => e.Name != "_id"))
                    Flatten($"{path}.{element.Name}", element.Value, leaves);
                break;
            case BsonArray array when array.Any(v => v.IsBsonDocument || v.IsBsonArray):
                for (var i = 0; i < array.Count; i++)
                {
                    //the key of a user field is already in its path; repeating it as a field shows a change of nothing
                    var element = array[i] is BsonDocument document && IsKeyed(document)
                        ? new BsonDocument(document.Where(e => e.Name is not ("Key" or "StoreId")))
                        : array[i];
                    Flatten($"{path}[{ElementKey(array[i], i)}]", element, leaves);
                }
                break;
            default:
                //a scalar, or a list of scalars kept as one value; when element ids repeat, the first wins
                leaves.TryAdd(path, Format(value));
                break;
        }
    }

    private static string ElementKey(BsonValue element, int index)
    {
        if (element is not BsonDocument document)
            return index.ToString();
        if (HasId(document, out var id))
            return $"id={id}";
        if (!IsKeyed(document))
            return index.ToString();
        var key = document["Key"].AsString;
        return document.TryGetValue("StoreId", out var storeId) && storeId.IsString && storeId.AsString != ""
            ? $"{key}@{storeId.AsString}"
            : key;
    }

    private static bool HasId(BsonDocument document, out BsonValue id)
    {
        return (document.TryGetValue("_id", out id) || document.TryGetValue("Id", out id)) && !id.IsBsonNull;
    }

    //a key/value pair such as a user field: matched by its key (once per store) instead of its position
    private static bool IsKeyed(BsonDocument document)
    {
        return !HasId(document, out _) && document.TryGetValue("Key", out var key) && key.IsString && key.AsString != "";
    }

    private static BsonDocument TryParse(string json)
    {
        try
        {
            return BsonDocument.Parse(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Format(BsonValue value)
    {
        if (value.IsString)
            return value.AsString;
        //a Guid is stored as binary (CSharpLegacy, see MongoDBStartupBase) but is a value like any other
        if (value is BsonBinaryData binary)
            return binary.SubType switch {
                BsonBinarySubType.UuidLegacy => GuidConverter.FromBytes(binary.Bytes, GuidRepresentation.CSharpLegacy).ToString(),
                BsonBinarySubType.UuidStandard => GuidConverter.FromBytes(binary.Bytes, GuidRepresentation.Standard).ToString(),
                _ => Binary
            };
        //to the second: a form that posts a date back drops its milliseconds, which is no change to a person
        if (value.IsValidDateTime)
            return value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        if (value.IsBsonArray)
            return value.ToJson();
        return value.ToString();
    }
}
