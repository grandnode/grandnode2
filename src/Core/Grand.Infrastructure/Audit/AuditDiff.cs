using Grand.Data.Audit;
using Grand.Domain.Audit;
using System.Text.RegularExpressions;

namespace Grand.Infrastructure.Audit;

/// <summary>
///     Compares two snapshots of an entity (see <see cref="AuditSnapshot" />) and lists the paths whose values differ
/// </summary>
public static partial class AuditDiff
{
    public const int MaxValueLength = 1024;

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
    public static IList<AuditChange> Compare(string entityType, IDictionary<string, string> before,
        IDictionary<string, string> after, ISet<string> ignoredPaths)
    {
        var changes = new List<AuditChange>();
        if (after == null)
            return changes;

        before ??= new Dictionary<string, string>();
        foreach (var path in after.Keys.Concat(before.Keys.Where(k => !after.ContainsKey(k))))
        {
            //an empty value compares like a missing field, so an insert lists only what was set
            var oldValue = ValueOf(before, path);
            var newValue = ValueOf(after, path);
            if ((oldValue == newValue && oldValue != AuditSnapshot.Binary) || ignoredPaths.Any(p => IsUnder(path, p)))
                continue;

            //settings JSON that does not parse stays one opaque field and may hold secrets
            var masked = IsSensitive(path) || (entityType == "Setting" && path == AuditSnapshot.SettingMetadata);
            changes.Add(masked
                ? new AuditChange { Path = path, Masked = true }
                : Change(path, oldValue, newValue));
        }

        return changes;
    }

    /// <summary>
    ///     A display label for the entity: the first non-empty of Name, Title, Email, OrderNumber, Code, SystemName.
    /// </summary>
    public static string EntityName(IDictionary<string, string> snapshot)
    {
        return snapshot == null ? null : NameFields.Select(field => ValueOf(snapshot, field)).FirstOrDefault(v => v != null);
    }

    private static string ValueOf(IDictionary<string, string> snapshot, string path)
    {
        return snapshot.TryGetValue(path, out var value) && !string.IsNullOrEmpty(value) ? value : null;
    }

    private static bool IsUnder(string path, string parent)
    {
        return path == parent
               || path.StartsWith(parent + ".", StringComparison.Ordinal)
               || path.StartsWith(parent + "[", StringComparison.Ordinal);
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
