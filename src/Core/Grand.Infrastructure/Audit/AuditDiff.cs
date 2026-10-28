using Grand.Domain.Audit;
using MongoDB.Bson;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Grand.Infrastructure.Audit;

/// <summary>
///     Compares two versions of a stored document and lists the changed fields, provider-independent.
/// </summary>
public static partial class AuditDiff
{
    public const int MaxValueLength = 1024;

    private static readonly string[] NameFields = ["Name", "Title", "Email", "OrderNumber", "Code", "SystemName"];

    [GeneratedRegex("password|salt|hash|secret|api_?key|token|privatekey", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveName();

    /// <summary>
    ///     Lists the changed leaf fields. <paramref name="before" /> is null for an insert, <paramref name="after" />
    ///     is null for a delete (which lists nothing).
    /// </summary>
    public static IList<AuditChange> Compare(string entityType, BsonDocument before, BsonDocument after,
        ISet<string> ignoredPaths)
    {
        var changes = new List<AuditChange>();
        if (after == null)
            return changes;

        before ??= new BsonDocument();
        if (entityType == "Setting")
        {
            before = ExpandMetadata(before);
            after = ExpandMetadata(after);
        }

        CompareDocuments(string.Empty, false, before, after, ignoredPaths, changes);
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

    private static BsonDocument ExpandMetadata(BsonDocument document)
    {
        if (!document.TryGetValue("Metadata", out var metadata) || !metadata.IsString)
            return document;

        BsonDocument parsed;
        try
        {
            parsed = BsonDocument.Parse(metadata.AsString);
        }
        catch (Exception)
        {
            //the raw text may hold secrets; a hash still shows whether it changed
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(metadata.AsString)))[..12];
            var masked = document.DeepClone().AsBsonDocument;
            masked["Metadata"] = $"<unreadable settings, sha256 {hash}>";
            return masked;
        }

        var copy = document.DeepClone().AsBsonDocument;
        copy["Metadata"] = parsed;
        return copy;
    }

    private static void CompareDocuments(string path, bool masked, BsonDocument before, BsonDocument after,
        ISet<string> ignoredPaths, List<AuditChange> changes)
    {
        var names = after.Names.Concat(before.Names.Where(n => !after.Contains(n))).ToList();
        foreach (var name in names)
        {
            if (path.Length == 0 && name == "_id")
                continue;

            before.TryGetValue(name, out var b);
            after.TryGetValue(name, out var a);
            var childPath = path.Length == 0 ? name : $"{path}.{name}";
            CompareValues(childPath, masked || SensitiveName().IsMatch(name), b, a, ignoredPaths, changes);
        }
    }

    private static void CompareValues(string path, bool masked, BsonValue before, BsonValue after,
        ISet<string> ignoredPaths, List<AuditChange> changes)
    {
        if (ignoredPaths.Contains(path))
            return;
        if (before == null && after == null)
            return;

        //null next to a container compares like a missing side, so its leaves are listed (and masked) one by one
        if (before is { IsBsonNull: true } && after is { IsBsonDocument: true } or { IsBsonArray: true }) before = null;
        if (after is { IsBsonNull: true } && before is { IsBsonDocument: true } or { IsBsonArray: true }) after = null;

        //a missing side compares as an empty container, so an insert or a removal lists the leaves
        if (before == null && after.IsBsonDocument) before = new BsonDocument();
        if (after == null && before.IsBsonDocument) after = new BsonDocument();
        if (before == null && after.IsBsonArray) before = new BsonArray();
        if (after == null && before.IsBsonArray) after = new BsonArray();

        if ((before == null && after.IsBsonNull) || (after == null && before.IsBsonNull))
            return;

        if (before is { IsBsonDocument: true } && after is { IsBsonDocument: true })
        {
            CompareDocuments(path, masked, before.AsBsonDocument, after.AsBsonDocument, ignoredPaths, changes);
            return;
        }

        if (before is { IsBsonArray: true } && after is { IsBsonArray: true })
        {
            CompareArrays(path, masked, before.AsBsonArray, after.AsBsonArray, ignoredPaths, changes);
            return;
        }

        if (Equals(before, after))
            return;

        AddChange(changes, path, masked || ContainsSensitive(before) || ContainsSensitive(after), Format(before),
            Format(after));
    }

    private static void CompareArrays(string path, bool masked, BsonArray before, BsonArray after,
        ISet<string> ignoredPaths, List<AuditChange> changes)
    {
        var all = before.Concat(after).ToList();
        if (all.Count == 0)
            return;

        if (all.All(v => v.IsBsonDocument && ElementId(v.AsBsonDocument) != null))
        {
            var beforeById = ById(before);
            var afterById = ById(after);
            foreach (var id in afterById.Keys.Concat(beforeById.Keys.Where(k => !afterById.ContainsKey(k))))
            {
                var elementPath = $"{path}[id={id}]";
                beforeById.TryGetValue(id, out var b);
                afterById.TryGetValue(id, out var a);
                if (b != null && a != null)
                    CompareDocuments(elementPath, masked, b, a, ignoredPaths, changes);
                else if (!ignoredPaths.Contains(elementPath))
                    AddChange(changes, elementPath, masked || ContainsSensitive(b) || ContainsSensitive(a),
                        b == null ? null : Summary(b, id), a == null ? null : Summary(a, id));
            }

            return;
        }

        //key/value pairs (user fields): matched by key, and masked by the key's name - the leaf is always "Value",
        //so a password token or a 2FA secret would otherwise be stored in clear
        if (all.All(v => v.IsBsonDocument && ElementKey(v.AsBsonDocument) != null))
        {
            var beforeByKey = ByKey(before);
            var afterByKey = ByKey(after);
            foreach (var key in afterByKey.Keys.Concat(beforeByKey.Keys.Where(k => !afterByKey.ContainsKey(k))))
            {
                var elementPath = $"{path}[{key}]";
                var keyMasked = masked || SensitiveName().IsMatch(key);
                beforeByKey.TryGetValue(key, out var b);
                afterByKey.TryGetValue(key, out var a);
                if (b != null && a != null)
                    CompareDocuments(elementPath, keyMasked, b, a, ignoredPaths, changes);
                else if (!ignoredPaths.Contains(elementPath))
                    AddChange(changes, elementPath, keyMasked || ContainsSensitive(b) || ContainsSensitive(a),
                        b == null ? null : PairValue(b), a == null ? null : PairValue(a));
            }

            return;
        }

        if (all.All(v => !v.IsBsonDocument && !v.IsBsonArray))
        {
            var removed = before.Where(v => !after.Contains(v)).Select(Format).ToList();
            var added = after.Where(v => !before.Contains(v)).Select(Format).ToList();
            if (removed.Count == 0 && added.Count == 0)
                return;

            AddChange(changes, path, masked,
                removed.Count > 0 ? "removed: " + string.Join(", ", removed) : null,
                added.Count > 0 ? "added: " + string.Join(", ", added) : null);
            return;
        }

        for (var i = 0; i < Math.Max(before.Count, after.Count); i++)
            CompareValues($"{path}[{i}]", masked, i < before.Count ? before[i] : null,
                i < after.Count ? after[i] : null, ignoredPaths, changes);
    }

    private static string ElementId(BsonDocument element)
    {
        if (element.TryGetValue("_id", out var id) || element.TryGetValue("Id", out id))
            return id.IsBsonNull ? null : id.ToString();
        return null;
    }

    //the first element wins when ids repeat; a malformed document must not lose the whole batch
    private static Dictionary<string, BsonDocument> ById(BsonArray array)
    {
        var byId = new Dictionary<string, BsonDocument>();
        foreach (var element in array.Select(v => v.AsBsonDocument))
            byId.TryAdd(ElementId(element), element);
        return byId;
    }

    private static bool ContainsSensitive(BsonValue value)
    {
        return value switch {
            BsonDocument document => document.Elements.Any(e =>
                SensitiveName().IsMatch(e.Name) || ContainsSensitive(e.Value)),
            BsonArray array => array.Any(ContainsSensitive),
            _ => false
        };
    }

    private static string ElementKey(BsonDocument element)
    {
        if (element.Contains("_id") || element.Contains("Id"))
            return null;
        if (!element.TryGetValue("Key", out var key) || !key.IsString || string.IsNullOrEmpty(key.AsString))
            return null;

        //the same key can exist once per store
        return element.TryGetValue("StoreId", out var storeId) && storeId.IsString && storeId.AsString != ""
            ? $"{key.AsString}@{storeId.AsString}"
            : key.AsString;
    }

    private static Dictionary<string, BsonDocument> ByKey(BsonArray array)
    {
        var byKey = new Dictionary<string, BsonDocument>();
        foreach (var element in array.Select(v => v.AsBsonDocument))
            byKey.TryAdd(ElementKey(element), element);
        return byKey;
    }

    private static string PairValue(BsonDocument element)
    {
        return element.TryGetValue("Value", out var value) ? Format(value) : element.ToJson();
    }

    private static string Summary(BsonDocument element, string id)
    {
        return EntityName(element) ?? id;
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
        if (value.IsBsonDocument || value.IsBsonArray)
            return value.ToJson();
        return value.ToString();
    }

    private static void AddChange(List<AuditChange> changes, string path, bool masked, string oldValue,
        string newValue)
    {
        if (masked)
        {
            changes.Add(new AuditChange { Path = path, Masked = true });
            return;
        }

        var change = new AuditChange {
            Path = path,
            OldValue = Truncate(oldValue, out var oldTruncated),
            NewValue = Truncate(newValue, out var newTruncated)
        };
        change.Truncated = oldTruncated || newTruncated;
        changes.Add(change);
    }

    private static string Truncate(string value, out bool truncated)
    {
        truncated = value is { Length: > MaxValueLength };
        return truncated ? value[..MaxValueLength] : value;
    }
}
