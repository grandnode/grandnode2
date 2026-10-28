using Grand.Domain.Audit;
using MongoDB.Bson;
using System.Text.RegularExpressions;

namespace Grand.Infrastructure.Audit;

/// <summary>
///     Compares two versions of a stored document field by field, provider-independent: both are flattened to
///     "path → value" and the paths whose values differ are listed.
/// </summary>
public static partial class AuditDiff
{
    public const int MaxValueLength = 1024;

    private const string SettingMetadata = "Metadata";

    private static readonly string[] NameFields = ["Name", "Title", "Email", "OrderNumber", "Code", "SystemName"];

    [GeneratedRegex("password|salt|hash|secret|api_?key|token|privatekey", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveName();

    /// <summary>
    ///     Whether a field, path or argument name looks like it holds a secret, whose value is never stored
    /// </summary>
    public static bool IsSensitive(string name)
    {
        return SensitiveName().IsMatch(name);
    }

    /// <summary>
    ///     Lists the changed fields. <paramref name="before" /> is null for an insert, <paramref name="after" />
    ///     is null for a delete (which lists nothing).
    /// </summary>
    public static IList<AuditChange> Compare(string entityType, BsonDocument before, BsonDocument after,
        ISet<string> ignoredPaths)
    {
        var changes = new List<AuditChange>();
        if (after == null)
            return changes;

        var isSetting = entityType == "Setting";
        var old = Flatten(before, isSetting);
        var current = Flatten(after, isSetting);
        foreach (var path in current.Keys.Concat(old.Keys.Where(k => !current.ContainsKey(k))))
        {
            old.TryGetValue(path, out var oldValue);
            current.TryGetValue(path, out var newValue);
            if (Equals(oldValue, newValue) || ignoredPaths.Any(p => IsUnder(path, p)))
                continue;

            //settings JSON that does not parse stays one opaque field and may hold secrets
            var masked = IsSensitive(path) || (isSetting && path == SettingMetadata);
            changes.Add(masked
                ? new AuditChange { Path = path, Masked = true }
                : Change(path, Format(oldValue), Format(newValue)));
        }

        return changes;
    }

    /// <summary>
    ///     A display label for the entity: the first non-empty of Name, Title, Email, OrderNumber, Code, SystemName.
    /// </summary>
    public static string EntityName(BsonDocument document)
    {
        if (document == null)
            return null;

        foreach (var field in NameFields)
            if (document.TryGetValue(field, out var value) && value.IsString && !string.IsNullOrEmpty(value.AsString))
                return value.AsString;

        return null;
    }

    private static Dictionary<string, BsonValue> Flatten(BsonDocument document, bool isSetting)
    {
        var leaves = new Dictionary<string, BsonValue>();
        if (document == null)
            return leaves;

        foreach (var element in document)
        {
            if (element.Name == "_id")
                continue;

            var value = element.Value;
            //a setting keeps its fields as JSON text, diffed field by field
            if (isSetting && element.Name == SettingMetadata && value.IsString)
                value = TryParse(value.AsString) ?? value;

            Flatten(element.Name, value, leaves);
        }

        return leaves;
    }

    private static void Flatten(string path, BsonValue value, Dictionary<string, BsonValue> leaves)
    {
        switch (value)
        {
            //null and empty compare like a missing field, so an insert lists only what was set
            case BsonNull or BsonString { Value: "" } or BsonArray { Count: 0 }:
                break;
            //an element's id is already in its path
            case BsonDocument document:
                foreach (var element in document.Where(e => e.Name != "_id"))
                    Flatten($"{path}.{element.Name}", element.Value, leaves);
                break;
            case BsonArray array when array.Any(v => v.IsBsonDocument || v.IsBsonArray):
                for (var i = 0; i < array.Count; i++)
                    Flatten($"{path}[{ElementKey(array[i], i)}]", array[i], leaves);
                break;
            default:
                //a scalar, or a list of scalars kept as one value; when element ids repeat, the first wins
                leaves.TryAdd(path, value);
                break;
        }
    }

    //an array element is matched by its id, else by its key (user fields, once per store), else by position;
    //the key is part of the path, so a user field named like a secret is masked
    private static string ElementKey(BsonValue element, int index)
    {
        if (element is not BsonDocument document)
            return index.ToString();
        if ((document.TryGetValue("_id", out var id) || document.TryGetValue("Id", out id)) && !id.IsBsonNull)
            return $"id={id}";
        if (!document.TryGetValue("Key", out var key) || !key.IsString || key.AsString == "")
            return index.ToString();
        return document.TryGetValue("StoreId", out var storeId) && storeId.IsString && storeId.AsString != ""
            ? $"{key.AsString}@{storeId.AsString}"
            : key.AsString;
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

    private static bool IsUnder(string path, string parent)
    {
        return path == parent
               || path.StartsWith(parent + ".", StringComparison.Ordinal)
               || path.StartsWith(parent + "[", StringComparison.Ordinal);
    }

    private static string Format(BsonValue value)
    {
        if (value == null || value.IsBsonNull)
            return null;
        if (value.IsString)
            return value.AsString;
        if (value.IsBsonBinaryData)
            return $"<binary {value.AsBsonBinaryData.Bytes.Length} bytes>";
        if (value.IsValidDateTime)
            return value.ToUniversalTime().ToString("o");
        if (value.IsBsonArray)
            return value.ToJson();
        return value.ToString();
    }

    private static AuditChange Change(string path, string oldValue, string newValue)
    {
        var change = new AuditChange {
            Path = path,
            OldValue = Truncate(oldValue, out var oldTruncated),
            NewValue = Truncate(newValue, out var newTruncated)
        };
        change.Truncated = oldTruncated || newTruncated;
        return change;
    }

    private static string Truncate(string value, out bool truncated)
    {
        truncated = value is { Length: > MaxValueLength };
        return truncated ? value[..MaxValueLength] : value;
    }
}
